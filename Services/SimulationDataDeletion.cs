using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Services;

public static class SimulationDataDeletion
{
    public static async Task DeleteSimulationProjectAsync(
        AppDbContext db,
        int simulationProjectId,
        CancellationToken cancellationToken = default)
    {
        var experimentRuns = await db.ExperimentRuns
            .Where(run => run.SimulationProjectId == simulationProjectId)
            .ToListAsync(cancellationToken);

        if (experimentRuns.Count > 0)
        {
            db.ExperimentRuns.RemoveRange(experimentRuns);
            await db.SaveChangesAsync(cancellationToken);
        }

        var project = await db.SimulationProjects
            .Include(item => item.Agents)
            .Include(item => item.Steps)
            .Include(item => item.TrustSnapshots)
            .Include(item => item.Metrics)
            .FirstOrDefaultAsync(item => item.Id == simulationProjectId, cancellationToken);

        if (project is null)
        {
            return;
        }

        db.SimulationProjects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task DeleteExperimentAsync(
        AppDbContext db,
        int experimentId,
        CancellationToken cancellationToken = default)
    {
        var cleanupService = new ExperimentCleanupService(db);
        await cleanupService.DeleteExperimentsAsync([experimentId], cancellationToken);
    }
}
