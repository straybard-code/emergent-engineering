using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Experiments;

public sealed class DeleteModel(AppDbContext db, ExperimentCleanupService cleanupService, ParameterSweepRunner parameterSweepRunner) : PageModel
{
    public Experiment? Experiment { get; private set; }
    public int ActualRunCount { get; private set; }
    public int SimulationProjectCount { get; private set; }
    public int SimulationStepCount { get; private set; }
    public int AgentCount { get; private set; }
    public int AgentActionCount { get; private set; }
    public int TrustSnapshotCount { get; private set; }
    public int ParameterSweepRunCount { get; private set; }
    public bool CanDelete => Experiment is not null && !IndexModel.IsDeletionBlocked(Experiment.Status);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Experiment = await db.Experiments.FirstOrDefaultAsync(item => item.Id == id);
        if (Experiment is null)
        {
            return RedirectToPage("/Experiments/Index");
        }

        await LoadCountsAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        var experiment = await db.Experiments.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (experiment is null)
        {
            return RedirectToPage("/Experiments/Index");
        }

        if (IndexModel.IsDeletionBlocked(experiment.Status))
        {
            TempData["CleanupMessage"] = "実行中のExperimentは削除できません。";
            return RedirectToPage("/Experiments/Index");
        }

        var result = await cleanupService.DeleteExperimentsAsync(
            [id],
            async (sweepId, innerCancellationToken) =>
            {
                await parameterSweepRunner.RecalculateSweepStatusAsync(sweepId, innerCancellationToken);
            },
            cancellationToken);

        TempData["CleanupMessage"] = result.ToJapaneseMessage();
        return RedirectToPage("/Experiments/Index");
    }

    private async Task LoadCountsAsync(int experimentId)
    {
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync();

        ActualRunCount = await db.ExperimentRuns.CountAsync(run => run.ExperimentId == experimentId);
        SimulationProjectCount = simulationProjectIds.Count;
        SimulationStepCount = simulationProjectIds.Count == 0
            ? 0
            : await db.SimulationSteps.CountAsync(step => simulationProjectIds.Contains(step.SimulationProjectId));
        AgentCount = simulationProjectIds.Count == 0
            ? 0
            : await db.Agents.CountAsync(agent => simulationProjectIds.Contains(agent.SimulationProjectId));
        AgentActionCount = simulationProjectIds.Count == 0
            ? 0
            : await db.AgentActions.CountAsync(action => simulationProjectIds.Contains(action.SimulationProjectId));
        TrustSnapshotCount = simulationProjectIds.Count == 0
            ? 0
            : await db.TrustSnapshots.CountAsync(snapshot => simulationProjectIds.Contains(snapshot.SimulationProjectId));
        ParameterSweepRunCount = await db.ParameterSweepRuns.CountAsync(run => run.ExperimentId == experimentId);
    }
}
