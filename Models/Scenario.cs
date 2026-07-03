using System.ComponentModel.DataAnnotations;

namespace EmergentEngineering.Models;

public sealed class Scenario
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    [Required]
    public string Purpose { get; set; } = "";

    [Required]
    public string BoundaryConditions { get; set; } = "";

    [Required]
    public string KpiDefinition { get; set; } = "";

    [Range(1, 20)]
    public int AgentCount { get; set; }

    [Range(1, 100)]
    public int TotalSteps { get; set; }

    [Range(1, 20)]
    public int RunCount { get; set; }

    [Required]
    [StringLength(40)]
    public string LlmProvider { get; set; } = LlmDefaults.Provider;

    [Required]
    [StringLength(120)]
    public string LlmModel { get; set; } = LlmDefaults.MockModel;

    [Range(0, 1)]
    public double InformationSharingLevel { get; set; } = BoundaryParameterDefaults.Level;

    [Range(0, 1)]
    public double CooperationLevel { get; set; } = BoundaryParameterDefaults.Level;

    [Range(0, 1)]
    public double CompetitionLevel { get; set; } = BoundaryParameterDefaults.Level;

    [Range(0, 1)]
    public double PsychologicalSafetyLevel { get; set; } = BoundaryParameterDefaults.Level;

    [Range(0, 1)]
    public double LearningOrientationLevel { get; set; } = BoundaryParameterDefaults.Level;

    [Range(0, 1)]
    public double CustomerOrientationLevel { get; set; } = BoundaryParameterDefaults.Level;

    [Range(0, 1)]
    public double ShortTermResultPressureLevel { get; set; } = BoundaryParameterDefaults.Level;

    [Range(0, 1)]
    public double EffectiveTrustThreshold { get; set; } = BoundaryParameterDefaults.EffectiveTrustThreshold;

    [Range(0, 1)]
    public double KnowledgeStock { get; set; } = KnowledgeDefaults.Stock;

    [Range(0, 1)]
    public double KnowledgeDiversity { get; set; } = KnowledgeDefaults.Diversity;

    [Range(0, 1)]
    public double ExternalShockLevel { get; set; } = KnowledgeDefaults.ExternalShockLevel;

    [Range(0, 1)]
    public double CrossDomainExposure { get; set; } = KnowledgeDefaults.CrossDomainExposure;

    [Range(0, 1)]
    public double RewiringSensitivity { get; set; } = KnowledgeDefaults.RewiringSensitivity;

    public bool EnableExternalShock { get; set; } = KnowledgeDefaults.EnableExternalShock;

    [Range(0, 100)]
    public int ShockStep { get; set; } = KnowledgeDefaults.ShockStep;

    [Required]
    [StringLength(40)]
    public string ShockType { get; set; } = KnowledgeDefaults.ShockType;

    public string ShockDescription { get; set; } = KnowledgeDefaults.ShockDescription;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<Experiment> Experiments { get; set; } = [];
}
