using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class SimulationFactory
{
    private static readonly string[] Roles =
    [
        "Coordinator",
        "Analyst",
        "Builder",
        "Critic",
        "Customer Advocate",
        "Operations Lead",
        "Researcher",
        "Integrator"
    ];

    public static SimulationProject CreateProject(CreateSimulationRequest request)
    {
        var project = new SimulationProject
        {
            Name = request.Name.Trim(),
            Purpose = request.Purpose.Trim(),
            BoundaryConditions = request.BoundaryConditions.Trim(),
            KpiDefinition = request.KpiDefinition.Trim(),
            AgentCount = request.AgentCount,
            TotalSteps = request.TotalSteps,
            CurrentStep = 0,
            Status = SimulationStatus.Created,
            Phase = SimulationPhase.Forming,
            CreatedAt = DateTime.UtcNow
        };

        for (var i = 1; i <= request.AgentCount; i++)
        {
            project.Agents.Add(new Agent
            {
                Name = $"Agent {i}",
                Role = Roles[(i - 1) % Roles.Length],
                Memory = "Initial memory: no shared history yet.",
                TrustJson = "{}",
                PositionX = Math.Round(Math.Cos(i * Math.PI * 2 / request.AgentCount), 3),
                PositionY = Math.Round(Math.Sin(i * Math.PI * 2 / request.AgentCount), 3)
            });
        }

        return project;
    }
}
