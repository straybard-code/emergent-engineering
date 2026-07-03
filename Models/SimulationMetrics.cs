namespace EmergentEngineering.Models;

public sealed class SimulationMetrics
{
    public int Id { get; set; }
    public int SimulationProjectId { get; set; }
    public SimulationProject? SimulationProject { get; set; }
    public string FinalPhase { get; set; } = SimulationPhase.Forming;
    public double AverageTrust { get; set; }
    public double NetworkDensity { get; set; }
    public int IsolatedAgentCount { get; set; }
    public string HubAgentName { get; set; } = "-";
    public double HubScore { get; set; }
    public double ShareInfoRate { get; set; }
    public double ProposeIdeaRate { get; set; }
    public double CriticizeSupportRatio { get; set; }
    public int? StepsToEmergent { get; set; }
    public int? StepsToLearning { get; set; }
    public int PhaseChangeCount { get; set; }
    public double PhaseStability { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
