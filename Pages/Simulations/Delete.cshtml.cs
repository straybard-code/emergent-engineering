using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Simulations;

public sealed class DeleteModel(AppDbContext db) : PageModel
{
    public SimulationProject? Project { get; private set; }
    public int ExperimentRunCount { get; private set; }
    public int AgentCount { get; private set; }
    public int AgentActionCount { get; private set; }
    public int SimulationStepCount { get; private set; }
    public int TrustSnapshotCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Project = await db.SimulationProjects.FirstOrDefaultAsync(item => item.Id == id);
        if (Project is null)
        {
            return RedirectToPage("/Index");
        }

        await LoadCountsAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var project = await db.SimulationProjects.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (project is null)
        {
            return RedirectToPage("/Index");
        }

        await SimulationDataDeletion.DeleteSimulationProjectAsync(db, id);
        return RedirectToPage("/Index");
    }

    private async Task LoadCountsAsync(int simulationProjectId)
    {
        ExperimentRunCount = await db.ExperimentRuns.CountAsync(run => run.SimulationProjectId == simulationProjectId);
        AgentCount = await db.Agents.CountAsync(agent => agent.SimulationProjectId == simulationProjectId);
        AgentActionCount = await db.AgentActions.CountAsync(action => action.SimulationProjectId == simulationProjectId);
        SimulationStepCount = await db.SimulationSteps.CountAsync(step => step.SimulationProjectId == simulationProjectId);
        TrustSnapshotCount = await db.TrustSnapshots.CountAsync(snapshot => snapshot.SimulationProjectId == simulationProjectId);
    }
}
