using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Experiments;

public sealed class DetailsModel(AppDbContext db) : PageModel
{
    [TempData]
    public string? ExperimentMessage { get; set; }

    public Experiment? Experiment { get; private set; }
    public List<ExperimentRun> Runs { get; private set; } = [];
    public List<PhaseDistributionRow> PhaseDistribution { get; private set; } = [];
    public List<PhaseTransitionRow> PhaseTransitions { get; private set; } = [];
    public List<TrustTrendPoint> TrustTrend { get; private set; } = [];
    public List<TrustStatePoint> TrustStateTrend { get; private set; } = [];
    public double AverageTrustOverall { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Experiment = await db.Experiments
            .Include(item => item.Scenario)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (Experiment is null)
        {
            return Page();
        }

        Runs = await db.ExperimentRuns
            .Include(run => run.SimulationProject)
            .Where(run => run.ExperimentId == id)
            .OrderBy(run => run.RunNo)
            .ToListAsync();

        AverageTrustOverall = Runs.Count == 0 ? 0 : Math.Round(Runs.Average(run => run.AverageTrust), 2);

        PhaseDistribution = GetPhaseDistribution(Runs);
        PhaseTransitions = await GetPhaseTransitionsAsync(id);
        TrustTrend = await GetTrustTrendAsync(id);
        TrustStateTrend = await GetTrustStateTrendAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostStopAsync(int id)
    {
        var experiment = await db.Experiments.FirstOrDefaultAsync(item => item.Id == id);
        if (experiment is null)
        {
            return RedirectToPage("/Experiments/Index");
        }

        if (experiment.Status == ExperimentStatus.Running)
        {
            experiment.Status = ExperimentStatus.StopRequested;
            await db.SaveChangesAsync();
            ExperimentMessage = "停止要求を受け付けました。現在実行中のRunが終わった後に停止します。";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteRunAsync(int id, int runId)
    {
        var run = await db.ExperimentRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == runId && item.ExperimentId == id);

        if (run is null)
        {
            return RedirectToPage(new { id });
        }

        await SimulationDataDeletion.DeleteSimulationProjectAsync(db, run.SimulationProjectId);
        ExperimentMessage = "Run と関連シミュレーションを削除しました。";
        return RedirectToPage(new { id });
    }

    private static List<PhaseDistributionRow> GetPhaseDistribution(IReadOnlyCollection<ExperimentRun> runs)
    {
        var phases = new[]
        {
            SimulationPhase.Forming,
            SimulationPhase.Learning,
            SimulationPhase.Emergent,
            SimulationPhase.Stable,
            SimulationPhase.Silo,
            SimulationPhase.Chaos,
            SimulationPhase.Collapse
        };

        return phases
            .Select(phase => new PhaseDistributionRow
            {
                Phase = phase,
                Count = runs.Count(run => run.FinalPhase == phase)
            })
            .ToList();
    }

    private async Task<List<PhaseTransitionRow>> GetPhaseTransitionsAsync(int experimentId)
    {
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync();

        var steps = await db.SimulationSteps
            .Where(step => simulationProjectIds.Contains(step.SimulationProjectId))
            .OrderBy(step => step.SimulationProjectId)
            .ThenBy(step => step.StepNo)
            .ToListAsync();

        var transitions = steps
            .GroupBy(step => step.SimulationProjectId)
            .SelectMany(group => group.Zip(group.Skip(1), (from, to) => new { from.Phase, ToPhase = to.Phase }))
            .GroupBy(item => new { item.Phase, item.ToPhase })
            .Select(group => new PhaseTransitionRow
            {
                FromPhase = group.Key.Phase,
                ToPhase = group.Key.ToPhase,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.FromPhase)
            .ThenBy(item => item.ToPhase)
            .ToList();

        return transitions;
    }

    private async Task<List<TrustTrendPoint>> GetTrustTrendAsync(int experimentId)
    {
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync();

        var actions = await db.AgentActions
            .Where(action => simulationProjectIds.Contains(action.SimulationProjectId) && action.TrustDelta.HasValue)
            .OrderBy(action => action.StepNo)
            .ToListAsync();

        return actions
            .GroupBy(action => action.StepNo)
            .Select(group => new TrustTrendPoint
            {
                StepNo = group.Key,
                AverageTrustDelta = Math.Round(group.Average(action => action.TrustDelta ?? 0), 2),
                Count = group.Count()
            })
            .OrderBy(point => point.StepNo)
            .ToList();
    }

    private async Task<List<TrustStatePoint>> GetTrustStateTrendAsync(int experimentId)
    {
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync();

        var snapshots = await db.TrustSnapshots
            .Where(snapshot => simulationProjectIds.Contains(snapshot.SimulationProjectId))
            .OrderBy(snapshot => snapshot.StepNo)
            .ToListAsync();

        return snapshots
            .GroupBy(snapshot => snapshot.StepNo)
            .Select(group => new TrustStatePoint
            {
                StepNo = group.Key,
                AverageTrust = Math.Round(group.Average(snapshot => snapshot.TrustValue), 2),
                AverageAbsTrust = Math.Round(group.Average(snapshot => Math.Abs(snapshot.TrustValue)), 2),
                Count = group.Count()
            })
            .OrderBy(point => point.StepNo)
            .ToList();
    }
}
