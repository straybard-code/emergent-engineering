namespace EmergentEngineering.Models;

public sealed class ExperimentRun
{
    public int Id { get; set; }
    public int ExperimentId { get; set; }
    public Experiment? Experiment { get; set; }
    public int SimulationProjectId { get; set; }
    public SimulationProject? SimulationProject { get; set; }
    public int RunNo { get; set; }
    public string FinalPhase { get; set; } = SimulationPhase.Forming;
    public double AverageTrust { get; set; }
    public double NetworkDensity { get; set; }
    public double EffectiveNetworkDensity { get; set; }
    public int StrongLinkCount { get; set; }
    public int WeakLinkCount { get; set; }
    public int ComponentCount { get; set; }
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
    public int CompletedSteps { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
