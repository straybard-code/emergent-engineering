using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public interface IExperimentExecutionService
{
    Task<Experiment?> RunExperimentAsync(int experimentId, CancellationToken cancellationToken = default);
    Task<Experiment?> RunExperimentAsync(int experimentId, string persistenceMode, CancellationToken cancellationToken = default);
    Task<Experiment?> RecalculateExperimentStatusAsync(int experimentId, CancellationToken cancellationToken = default);
}
