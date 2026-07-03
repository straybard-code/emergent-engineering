using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class RunModel(AppDbContext db, IExperimentExecutionService experimentExecutionService) : PageModel
{
    [TempData]
    public string? SweepMessage { get; set; }

    public ParameterSweep? Sweep { get; private set; }
    public string ScenarioName { get; private set; } = "-";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
        if (Sweep is null)
        {
            return RedirectToPage("/ParameterSweeps/Index");
        }

        ScenarioName = await db.Scenarios
            .Where(item => item.Id == Sweep.ScenarioId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync() ?? "-";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
        if (sweep is null)
        {
            return RedirectToPage("/ParameterSweeps/Index");
        }

        if (sweep.Status == ParameterSweepStatus.Running)
        {
            return RedirectToPage("/ParameterSweeps/Details", new { id });
        }

        var scenario = await db.Scenarios.FirstOrDefaultAsync(item => item.Id == sweep.ScenarioId);
        if (scenario is null)
        {
            sweep.Status = ParameterSweepStatus.Failed;
            await db.SaveChangesAsync();
            TempData["SweepMessage"] = "基準シナリオが見つからないため、スイープを開始できませんでした。";
            return RedirectToPage("/ParameterSweeps/Details", new { id });
        }

        sweep.Status = ParameterSweepStatus.Running;
        sweep.CompletedAt = null;
        await db.SaveChangesAsync();

        try
        {
            await DeleteExistingResultsAsync(sweep.Id);

            foreach (var parameterValue in BuildParameterValues(sweep.StartValue, sweep.EndValue, sweep.StepValue))
            {
                var latestStatus = await db.ParameterSweeps
                    .AsNoTracking()
                    .Where(item => item.Id == sweep.Id)
                    .Select(item => item.Status)
                    .FirstAsync();

                if (latestStatus == ParameterSweepStatus.StopRequested)
                {
                    sweep.Status = ParameterSweepStatus.Stopped;
                    await db.SaveChangesAsync();
                    TempData["SweepMessage"] = "停止要求を受け付けたため、現在のパラメータ値の処理境界でスイープを停止しました。";
                    return RedirectToPage("/ParameterSweeps/Details", new { id = sweep.Id });
                }

                var parameterLabel = BoundaryParameterNames.GetLabel(sweep.TargetParameter);
                var experiment = SimulationFactory.CreateExperimentFromScenario(
                    scenario,
                    $"{sweep.Name} {parameterLabel}={parameterValue:0.00}",
                    $"{sweep.Description} / {parameterLabel}={parameterValue:0.00}",
                    sweep.RunCountPerValue,
                    sweep.AgentCount,
                    sweep.TotalSteps,
                    sweep.LlmProvider,
                    sweep.LlmModel);

                if (!BoundaryParameterNames.TryApply(experiment, sweep.TargetParameter, parameterValue))
                {
                    throw new InvalidOperationException($"Unsupported target parameter: {sweep.TargetParameter}");
                }

                db.Experiments.Add(experiment);
                await db.SaveChangesAsync();

                await experimentExecutionService.RunExperimentAsync(experiment.Id);

                var experimentRuns = await db.ExperimentRuns
                    .Where(item => item.ExperimentId == experiment.Id)
                    .OrderBy(item => item.RunNo)
                    .ToListAsync();

                var finalPhaseSummary = BuildPhaseSummary(experimentRuns);
                var averageTrust = experimentRuns.Count == 0 ? 0 : Math.Round(experimentRuns.Average(item => item.AverageTrust), 2);
                var averageAbsTrust = await CalculateAverageAbsTrustAsync(experiment.Id);

                db.ParameterSweepRuns.Add(new ParameterSweepRun
                {
                    ParameterSweepId = sweep.Id,
                    ParameterValue = parameterValue,
                    ExperimentId = experiment.Id,
                    RunCount = sweep.RunCountPerValue,
                    FinalPhaseSummaryJson = JsonSerializer.Serialize(finalPhaseSummary),
                    AverageTrust = averageTrust,
                    AverageAbsTrust = averageAbsTrust,
                    CreatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }

            sweep.Status = ParameterSweepStatus.Completed;
            sweep.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return RedirectToPage("/ParameterSweeps/Details", new { id = sweep.Id });
        }
        catch
        {
            sweep.Status = ParameterSweepStatus.Failed;
            await db.SaveChangesAsync();
            SweepMessage = "スイープ実行中にエラーが発生したため、状態を Failed に更新しました。";
            return RedirectToPage("/ParameterSweeps/Details", new { id = sweep.Id });
        }
    }

    private async Task DeleteExistingResultsAsync(int parameterSweepId)
    {
        var existingRuns = await db.ParameterSweepRuns
            .Where(item => item.ParameterSweepId == parameterSweepId)
            .ToListAsync();

        var experimentIds = existingRuns
            .Select(item => item.ExperimentId)
            .Distinct()
            .ToList();

        foreach (var experimentId in experimentIds)
        {
            await SimulationDataDeletion.DeleteExperimentAsync(db, experimentId);
        }

        if (existingRuns.Count > 0)
        {
            db.ParameterSweepRuns.RemoveRange(existingRuns);
            await db.SaveChangesAsync();
        }
    }

    private async Task<double> CalculateAverageAbsTrustAsync(int experimentId)
    {
        var projects = await db.SimulationProjects
            .Where(item => item.ExperimentId == experimentId)
            .Select(item => new { item.Id, item.CurrentStep })
            .ToListAsync();

        if (projects.Count == 0)
        {
            return 0;
        }

        var projectIds = projects.Select(item => item.Id).ToList();
        var snapshots = await db.TrustSnapshots
            .Where(item => projectIds.Contains(item.SimulationProjectId))
            .ToListAsync();

        List<double> averages = [];

        foreach (var project in projects)
        {
            var finalSnapshots = snapshots
                .Where(item => item.SimulationProjectId == project.Id && item.StepNo == project.CurrentStep)
                .ToList();

            if (finalSnapshots.Count > 0)
            {
                averages.Add(finalSnapshots.Average(item => Math.Abs(item.TrustValue)));
                continue;
            }

            var trustMaps = await db.Agents
                .Where(item => item.SimulationProjectId == project.Id)
                .Select(item => item.TrustJson)
                .ToListAsync();

            var values = trustMaps
                .SelectMany(item => TrustJsonUtility.Deserialize(item).Values)
                .Select(Math.Abs)
                .ToList();

            averages.Add(values.Count == 0 ? 0 : values.Average());
        }

        return averages.Count == 0 ? 0 : Math.Round(averages.Average(), 2);
    }

    private static List<double> BuildParameterValues(double startValue, double endValue, double stepValue)
    {
        List<double> values = [];
        var start = decimal.Round((decimal)startValue, 2);
        var end = decimal.Round((decimal)endValue, 2);
        var step = decimal.Round((decimal)stepValue, 2);

        for (var value = start; value <= end + 0.0001m; value += step)
        {
            values.Add(Math.Round((double)value, 2));
        }

        return values;
    }

    private static Dictionary<string, int> BuildPhaseSummary(IEnumerable<ExperimentRun> runs)
    {
        var phases = new[]
        {
            SimulationPhase.Forming,
            SimulationPhase.Learning,
            SimulationPhase.Stable,
            SimulationPhase.Emergent,
            SimulationPhase.Silo,
            SimulationPhase.Chaos,
            SimulationPhase.Collapse
        };

        return phases.ToDictionary(
            phase => phase,
            phase => runs.Count(run => run.FinalPhase == phase));
    }
}
