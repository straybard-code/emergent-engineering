using EmergentEngineering.Data;

namespace EmergentEngineering.Services;

public static class SimulationDataDeletion
{
    public static async Task DeleteSimulationProjectAsync(
        AppDbContext db,
        int simulationProjectId,
        CancellationToken cancellationToken = default)
    {
        var cleanupService = new ExperimentCleanupService(db);
        await cleanupService.ForceDeleteSimulationAsync(simulationProjectId, cancellationToken: cancellationToken);
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
