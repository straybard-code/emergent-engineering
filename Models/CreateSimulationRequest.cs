using System.ComponentModel.DataAnnotations;

namespace EmergentEngineering.Models;

public sealed class CreateSimulationRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Required]
    public string Purpose { get; set; } = "";

    [Required]
    public string BoundaryConditions { get; set; } = "";

    [Required]
    public string KpiDefinition { get; set; } = "";

    [Range(1, 20)]
    public int AgentCount { get; set; } = 4;

    [Range(1, 100)]
    public int TotalSteps { get; set; } = 5;

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

    public bool EnableChallengeEvent { get; set; } = ChallengeDefaults.EnableChallengeEvent;

    [Required]
    [StringLength(40)]
    public string ChallengeType { get; set; } = ChallengeDefaults.ChallengeType;

    [Range(0, 100)]
    public int ChallengeStep { get; set; } = ChallengeDefaults.ChallengeStep;

    [Range(0, 1)]
    public double ChallengeLevel { get; set; } = ChallengeDefaults.ChallengeLevel;

    public string ChallengeDescription { get; set; } = ChallengeDefaults.ChallengeDescription;

    [Range(0, 1)]
    public double RequiredKnowledgeDiversity { get; set; } = ChallengeDefaults.RequiredKnowledgeDiversity;

    [Range(0, 1)]
    public double RequiredCrossDomainExposure { get; set; } = ChallengeDefaults.RequiredCrossDomainExposure;

    [Range(0, 1)]
    public double RequiredRewiringScore { get; set; } = ChallengeDefaults.RequiredRewiringScore;

    [Range(0, 1)]
    public double ExplorationTendency { get; set; } = SerendipityDefaults.ExplorationTendency;

    [Range(0, 1)]
    public double SerendipitySensitivity { get; set; } = SerendipityDefaults.SerendipitySensitivity;

    [Range(0, 1)]
    public double KnowledgeRecombinationRate { get; set; } = SerendipityDefaults.KnowledgeRecombinationRate;

    [Range(0, 1)]
    public double SerendipityThreshold { get; set; } = SerendipityDefaults.SerendipityThreshold;

    public bool EnableSerendipity { get; set; } = SerendipityDefaults.EnableSerendipity;
}
