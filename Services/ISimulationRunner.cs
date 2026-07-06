using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public interface ISimulationRunner
{
    Task<SimulationStep?> RunOneStepAsync(int simulationId, CancellationToken cancellationToken = default);
    Task<SimulationProject?> RunAllAsync(int simulationId, CancellationToken cancellationToken = default);
    Task<SimulationProject?> RunAllAsync(int simulationId, string persistenceMode, CancellationToken cancellationToken = default);
}
