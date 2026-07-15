using System.ComponentModel.DataAnnotations;

namespace EmergentEngineering.Models;

public sealed class Experiment : IThanksCoinSettings, IMutualRespectSettings, IIntellectualRespectSettings, IThinkingSpeedSettings, IValidatableObject
{
    public int Id { get; set; }
    public int? ScenarioId { get; set; }
    public Scenario? Scenario { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string BoundaryConditions { get; set; } = "";
    public string KpiDefinition { get; set; } = "";
    [Range(1, 100)]
    public int AgentCount { get; set; }
    [Range(1, 5000)]
    public int TotalSteps { get; set; }
    [Range(1, 100)]
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
    public bool EnableChallengeEvent { get; set; } = ChallengeDefaults.EnableChallengeEvent;
    public string ChallengeType { get; set; } = ChallengeDefaults.ChallengeType;
    public int ChallengeStep { get; set; } = ChallengeDefaults.ChallengeStep;
    public double ChallengeLevel { get; set; } = ChallengeDefaults.ChallengeLevel;
    public string ChallengeDescription { get; set; } = ChallengeDefaults.ChallengeDescription;
    public double RequiredKnowledgeDiversity { get; set; } = ChallengeDefaults.RequiredKnowledgeDiversity;
    public double RequiredCrossDomainExposure { get; set; } = ChallengeDefaults.RequiredCrossDomainExposure;
    public double RequiredRewiringScore { get; set; } = ChallengeDefaults.RequiredRewiringScore;
    public double ExplorationTendency { get; set; } = SerendipityDefaults.ExplorationTendency;
    public double SerendipitySensitivity { get; set; } = SerendipityDefaults.SerendipitySensitivity;
    public double KnowledgeRecombinationRate { get; set; } = SerendipityDefaults.KnowledgeRecombinationRate;
    public double SerendipityThreshold { get; set; } = SerendipityDefaults.SerendipityThreshold;
    public bool EnableSerendipity { get; set; } = SerendipityDefaults.EnableSerendipity;
    public bool EnableTrustDynamics { get; set; } = TrustDynamicsDefaults.EnableTrustDynamics;
    public double TrustGrowthRate { get; set; } = TrustDynamicsDefaults.TrustGrowthRate;
    public double TrustDecayRate { get; set; } = TrustDynamicsDefaults.TrustDecayRate;
    public double TrustSaturationStrength { get; set; } = TrustDynamicsDefaults.TrustSaturationStrength;
    public int TrustCapacity { get; set; } = TrustDynamicsDefaults.TrustCapacity;
    public double TrustCapacityPenalty { get; set; } = TrustDynamicsDefaults.TrustCapacityPenalty;
    public double DistrustPenalty { get; set; } = TrustDynamicsDefaults.DistrustPenalty;
    public double ConstructiveCriticismBonus { get; set; } = TrustDynamicsDefaults.ConstructiveCriticismBonus;
    public bool EnableThanksCoin { get; set; } = ThanksCoinDefaults.EnableThanksCoin;
    public double ThanksCoinRate { get; set; } = ThanksCoinDefaults.ThanksCoinRate;
    public double ThanksCoinRespectGain { get; set; } = ThanksCoinDefaults.ThanksCoinRespectGain;
    public double ThanksCoinTrustGain { get; set; } = ThanksCoinDefaults.ThanksCoinTrustGain;
    public double ThanksCoinReconfigurationGain { get; set; } = ThanksCoinDefaults.ThanksCoinReconfigurationGain;
    public double ThanksCoinPsychologicalSafetyGain { get; set; } = ThanksCoinDefaults.ThanksCoinPsychologicalSafetyGain;
    public double ThanksCoinBridgeGain { get; set; } = ThanksCoinDefaults.ThanksCoinBridgeGain;
    public double ThanksCoinPopularityBias { get; set; } = ThanksCoinDefaults.ThanksCoinPopularityBias;
    public double ThanksCoinDiversityBonus { get; set; } = ThanksCoinDefaults.ThanksCoinDiversityBonus;
    public double ThanksCoinChallengeBonus { get; set; } = ThanksCoinDefaults.ThanksCoinChallengeBonus;
    public double MutualRespectBase { get; set; } = MutualRespectDefaults.Base;
    public double MutualRespectGrowthRate { get; set; } = MutualRespectDefaults.GrowthRate;
    public double MutualRespectDecayRate { get; set; } = MutualRespectDefaults.DecayRate;
    public double MutualRespectDiversitySensitivity { get; set; } = MutualRespectDefaults.DiversitySensitivity;
    public double MutualRespectChallengeSensitivity { get; set; } = MutualRespectDefaults.ChallengeSensitivity;
    public double MutualRespectBridgeSensitivity { get; set; } = MutualRespectDefaults.BridgeSensitivity;
    public double MutualRespectPopularityPenalty { get; set; } = MutualRespectDefaults.PopularityPenalty;
    public bool EnableIntellectualRespect { get; set; } = IntellectualRespectDefaults.Enable;
    public double IntellectualRespectBase { get; set; } = IntellectualRespectDefaults.Base;
    public double IntellectualRespectGrowthRate { get; set; } = IntellectualRespectDefaults.GrowthRate;
    public double IntellectualRespectDecayRate { get; set; } = IntellectualRespectDefaults.DecayRate;
    public double IntellectualRespectDiversitySensitivity { get; set; } = IntellectualRespectDefaults.DiversitySensitivity;
    public double IntellectualRespectChallengeSensitivity { get; set; } = IntellectualRespectDefaults.ChallengeSensitivity;
    public double IntellectualRespectMentorshipSensitivity { get; set; } = IntellectualRespectDefaults.MentorshipSensitivity;
    public double IntellectualRespectEgoPenalty { get; set; } = IntellectualRespectDefaults.EgoPenalty;
    public double IntellectualRespectHierarchyPenalty { get; set; } = IntellectualRespectDefaults.HierarchyPenalty;
    public bool EnableThinkingSpeedModel { get; set; } = ThinkingSpeedDefaults.EnableThinkingSpeedModel;
    [Range(0.1, 30.0)]
    public double ThinkingSpeedBase { get; set; } = ThinkingSpeedDefaults.ThinkingSpeedBase;
    [Range(0.0, 1.0)]
    public double ThinkingSpeedDispersion { get; set; } = ThinkingSpeedDefaults.ThinkingSpeedDispersion;
    [Range(0.1, 30.0)]
    public double OrganizationalDecisionSpeed { get; set; } = ThinkingSpeedDefaults.OrganizationalDecisionSpeed;
    [Range(0.1, 30.0)]
    public double OrganizationalValidationSpeed { get; set; } = ThinkingSpeedDefaults.OrganizationalValidationSpeed;
    public string Status { get; set; } = ExperimentStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ExperimentRun> Runs { get; set; } = [];
    public List<SimulationProject> SimulationProjects { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in ConditionalEventValidation.ValidateExternalShock(
                     EnableExternalShock,
                     ShockType,
                     ShockStep,
                     ExternalShockLevel))
        {
            yield return result;
        }

        foreach (var result in ConditionalEventValidation.ValidateChallenge(
                     EnableChallengeEvent,
                     ChallengeType,
                     ChallengeStep,
                     ChallengeLevel,
                     RequiredKnowledgeDiversity,
                     RequiredCrossDomainExposure,
                     RequiredRewiringScore))
        {
            yield return result;
        }
    }
}
