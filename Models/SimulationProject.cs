namespace EmergentEngineering.Models;

public sealed class SimulationProject
{
    public int Id { get; set; }
    public int? ExperimentId { get; set; }
    public Experiment? Experiment { get; set; }
    public int? ExperimentRunId { get; set; }
    public string Name { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string BoundaryConditions { get; set; } = "";
    public string KpiDefinition { get; set; } = "";
    public int AgentCount { get; set; }
    public int TotalSteps { get; set; }
    public int CurrentStep { get; set; }
    public string LlmProvider { get; set; } = LlmDefaults.Provider;
    public string LlmModel { get; set; } = LlmDefaults.MockModel;
    public double InformationSharingLevel { get; set; } = BoundaryParameterDefaults.Level;
    public double CooperationLevel { get; set; } = BoundaryParameterDefaults.Level;
    public double CompetitionLevel { get; set; } = BoundaryParameterDefaults.Level;
    public double PsychologicalSafetyLevel { get; set; } = BoundaryParameterDefaults.Level;
    public double LearningOrientationLevel { get; set; } = BoundaryParameterDefaults.Level;
    public double CustomerOrientationLevel { get; set; } = BoundaryParameterDefaults.Level;
    public double ShortTermResultPressureLevel { get; set; } = BoundaryParameterDefaults.Level;
    public double EffectiveTrustThreshold { get; set; } = BoundaryParameterDefaults.EffectiveTrustThreshold;
    public string Status { get; set; } = SimulationStatus.Created;
    public string Phase { get; set; } = SimulationPhase.Forming;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public SimulationMetrics? Metrics { get; set; }
    public List<Agent> Agents { get; set; } = [];
    public List<SimulationStep> Steps { get; set; } = [];
    public List<TrustSnapshot> TrustSnapshots { get; set; } = [];
}
