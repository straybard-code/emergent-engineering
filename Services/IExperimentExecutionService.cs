using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public interface IExperimentExecutionService
{
    Task<Experiment?> RunExperimentAsync(int experimentId, CancellationToken cancellationToken = default);
}
