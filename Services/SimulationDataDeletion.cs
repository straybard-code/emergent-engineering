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
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync(cancellationToken);

        var experimentRuns = await db.ExperimentRuns
            .Where(run => run.ExperimentId == experimentId || simulationProjectIds.Contains(run.SimulationProjectId))
            .ToListAsync(cancellationToken);

        if (experimentRuns.Count > 0)
        {
            db.ExperimentRuns.RemoveRange(experimentRuns);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (simulationProjectIds.Count > 0)
        {
            var projects = await db.SimulationProjects
                .Include(item => item.Agents)
                .Include(item => item.Steps)
                .Include(item => item.TrustSnapshots)
                .Include(item => item.Metrics)
                .Where(item => simulationProjectIds.Contains(item.Id))
                .ToListAsync(cancellationToken);

            if (projects.Count > 0)
            {
                db.SimulationProjects.RemoveRange(projects);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        var experiment = await db.Experiments
            .FirstOrDefaultAsync(item => item.Id == experimentId, cancellationToken);

        if (experiment is null)
        {
            return;
        }

        db.Experiments.Remove(experiment);
        await db.SaveChangesAsync(cancellationToken);
    }
}
