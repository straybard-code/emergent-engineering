using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Experiments;

public sealed class DeleteModel(AppDbContext db) : PageModel
{
    public Experiment? Experiment { get; private set; }
    public int ActualRunCount { get; private set; }
    public int SimulationProjectCount { get; private set; }
    public int AgentActionCount { get; private set; }
    public int TrustSnapshotCount { get; private set; }

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

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var experiment = await db.Experiments.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (experiment is null)
        {
            return RedirectToPage("/Experiments/Index");
        }

        await SimulationDataDeletion.DeleteExperimentAsync(db, id);
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
        AgentActionCount = simulationProjectIds.Count == 0
            ? 0
            : await db.AgentActions.CountAsync(action => simulationProjectIds.Contains(action.SimulationProjectId));
        TrustSnapshotCount = simulationProjectIds.Count == 0
            ? 0
            : await db.TrustSnapshots.CountAsync(snapshot => simulationProjectIds.Contains(snapshot.SimulationProjectId));
    }
}
