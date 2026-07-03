namespace EmergentEngineering.Models;

public sealed class Experiment
{
    public int Id { get; set; }
    public int? ScenarioId { get; set; }
    public Scenario? Scenario { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string BoundaryConditions { get; set; } = "";
    public string KpiDefinition { get; set; } = "";
    public int AgentCount { get; set; }
    public int TotalSteps { get; set; }
    public int RunCount { get; set; }
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
    public double KnowledgeStock { get; set; } = KnowledgeDefaults.Stock;
    public double KnowledgeDiversity { get; set; } = KnowledgeDefaults.Diversity;
    public double ExternalShockLevel { get; set; } = KnowledgeDefaults.ExternalShockLevel;
    public double CrossDomainExposure { get; set; } = KnowledgeDefaults.CrossDomainExposure;
    public double RewiringSensitivity { get; set; } = KnowledgeDefaults.RewiringSensitivity;
    public bool EnableExternalShock { get; set; } = KnowledgeDefaults.EnableExternalShock;
    public int ShockStep { get; set; } = KnowledgeDefaults.ShockStep;
    public string ShockType { get; set; } = KnowledgeDefaults.ShockType;
    public string ShockDescription { get; set; } = KnowledgeDefaults.ShockDescription;
    public string Status { get; set; } = ExperimentStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ExperimentRun> Runs { get; set; } = [];
    public List<SimulationProject> SimulationProjects { get; set; } = [];
}
