using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public sealed class SimulationParameterApplier
{
    public bool ApplyParameter(Scenario scenario, string parameterName, double value)
    {
        return BoundaryParameterNames.TryApply(scenario, parameterName, value);
    }

    public bool ApplyParameter(Experiment experiment, string parameterName, double value)
    {
        return BoundaryParameterNames.TryApply(experiment, parameterName, value);
    }

    public bool ApplyParameter(SimulationProject simulationProject, string parameterName, double value)
    {
        return BoundaryParameterNames.TryApply(simulationProject, parameterName, value);
    }
}
