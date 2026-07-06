namespace EmergentEngineering.Models;

public static class AgentActionType
{
    public const string ShareInfo = "ShareInfo";
    public const string AskHelp = "AskHelp";
    public const string ProposeIdea = "ProposeIdea";
    public const string Criticize = "Criticize";
    public const string WorkAlone = "WorkAlone";
    public const string SupportOther = "SupportOther";
    public const string Wait = "Wait";

    public static readonly string[] All =
    [
        ShareInfo,
        AskHelp,
        ProposeIdea,
        Criticize,
        WorkAlone,
        SupportOther,
        Wait
    ];
}

public static class SimulationStatus
{
    public const string Created = "Created";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class ExperimentStatus
{
    public const string Created = "Created";
    public const string Running = "Running";
    public const string Failed = "Failed";
    public const string StopRequested = "StopRequested";
    public const string Stopped = "Stopped";
    public const string Completed = "Completed";
}

public static class LlmProviderType
{
    public const string Mock = "Mock";
    public const string OpenAI = "OpenAI";
}

public static class LlmDefaults
{
    public const string Provider = LlmProviderType.Mock;
    public const string MockModel = "mock-v1";
    public const string OpenAiRecommendedModel = "gpt-4o-mini";
    public static readonly string[] AvailableModels =
    [
        MockModel,
        "gpt-4o-mini",
        "gpt-4o",
        "gpt-5.5",
        "gpt-5.6"
    ];
}

public static class BoundaryParameterDefaults
{
    public const double Level = 0.5;
    public const double EffectiveTrustThreshold = 0.30;
}

public static class KnowledgeDefaults
{
    public const double Stock = 0.0;
    public const double Diversity = 0.3;
    public const double ExternalShockLevel = 0.0;
    public const double CrossDomainExposure = 0.0;
    public const double RewiringSensitivity = 0.5;
    public const bool EnableExternalShock = false;
    public const int ShockStep = 0;
    public const string ShockType = ShockTypes.None;
    public const string ShockDescription = "";
}

public static class ChallengeDefaults
{
    public const bool EnableChallengeEvent = false;
    public const string ChallengeType = ChallengeTypes.None;
    public const int ChallengeStep = 0;
    public const double ChallengeLevel = 0.0;
    public const string ChallengeDescription = "";
    public const double RequiredKnowledgeDiversity = 0.5;
    public const double RequiredCrossDomainExposure = 0.5;
    public const double RequiredRewiringScore = 0.3;
}

public static class SerendipityDefaults
{
    public const double ExplorationTendency = 0.3;
    public const double SerendipitySensitivity = 0.3;
    public const double KnowledgeRecombinationRate = 0.3;
    public const double SerendipityThreshold = 0.5;
    public const bool EnableSerendipity = true;
}

public static class TrustDynamicsDefaults
{
    public const bool EnableTrustDynamics = true;
    public const double TrustGrowthRate = 1.0;
    public const double TrustDecayRate = 0.002;
    public const double TrustSaturationStrength = 0.7;
    public const int TrustCapacity = 5;
    public const double TrustCapacityPenalty = 0.03;
    public const double DistrustPenalty = 0.05;
    public const double ConstructiveCriticismBonus = 0.01;
}

public static class ShockTypes
{
    public const string None = "None";
    public const string CrossDomainExpert = "CrossDomainExpert";
    public const string CustomerDemandShift = "CustomerDemandShift";
    public const string NewTechnology = "NewTechnology";
    public const string CompetitorMove = "CompetitorMove";
    public const string FailureIncident = "FailureIncident";
    public const string CultureShock = "CultureShock";

    public static readonly string[] All =
    [
        None,
        CrossDomainExpert,
        CustomerDemandShift,
        NewTechnology,
        CompetitorMove,
        FailureIncident,
        CultureShock
    ];
}

public static class ChallengeTypes
{
    public const string None = "None";
    public const string ExistingMethodFailure = "ExistingMethodFailure";
    public const string NewMarketRequirement = "NewMarketRequirement";
    public const string CrossFunctionalProblem = "CrossFunctionalProblem";
    public const string QualityCrisis = "QualityCrisis";
    public const string TechnologyShift = "TechnologyShift";
    public const string CustomerComplexityIncrease = "CustomerComplexityIncrease";

    public static readonly string[] All =
    [
        None,
        ExistingMethodFailure,
        NewMarketRequirement,
        CrossFunctionalProblem,
        QualityCrisis,
        TechnologyShift,
        CustomerComplexityIncrease
    ];
}

public static class BoundaryParameterNames
{
    public const string InformationSharingLevel = "InformationSharingLevel";
    public const string CooperationLevel = "CooperationLevel";
    public const string CompetitionLevel = "CompetitionLevel";
    public const string PsychologicalSafetyLevel = "PsychologicalSafetyLevel";
    public const string LearningOrientationLevel = "LearningOrientationLevel";
    public const string CustomerOrientationLevel = "CustomerOrientationLevel";
    public const string ShortTermResultPressureLevel = "ShortTermResultPressureLevel";
    public const string EffectiveTrustThreshold = "EffectiveTrustThreshold";

    public const string KnowledgeStock = "KnowledgeStock";
    public const string KnowledgeDiversity = "KnowledgeDiversity";
    public const string ExternalShockLevel = "ExternalShockLevel";
    public const string CrossDomainExposure = "CrossDomainExposure";
    public const string RewiringSensitivity = "RewiringSensitivity";

    public const string ExplorationTendency = "ExplorationTendency";
    public const string SerendipitySensitivity = "SerendipitySensitivity";
    public const string KnowledgeRecombinationRate = "KnowledgeRecombinationRate";
    public const string SerendipityThreshold = "SerendipityThreshold";

    public const string TrustGrowthRate = "TrustGrowthRate";
    public const string TrustDecayRate = "TrustDecayRate";
    public const string TrustSaturationStrength = "TrustSaturationStrength";
    public const string TrustCapacity = "TrustCapacity";
    public const string TrustCapacityPenalty = "TrustCapacityPenalty";
    public const string DistrustPenalty = "DistrustPenalty";
    public const string ConstructiveCriticismBonus = "ConstructiveCriticismBonus";

    public const string ChallengeLevel = "ChallengeLevel";
    public const string RequiredKnowledgeDiversity = "RequiredKnowledgeDiversity";
    public const string RequiredCrossDomainExposure = "RequiredCrossDomainExposure";
    public const string RequiredRewiringScore = "RequiredRewiringScore";

    public static readonly string[] BoundaryConditions =
    [
        InformationSharingLevel,
        CooperationLevel,
        CompetitionLevel,
        PsychologicalSafetyLevel,
        LearningOrientationLevel,
        CustomerOrientationLevel,
        ShortTermResultPressureLevel,
        EffectiveTrustThreshold
    ];

    public static readonly string[] KnowledgeAndRewiring =
    [
        KnowledgeStock,
        KnowledgeDiversity,
        ExternalShockLevel,
        CrossDomainExposure,
        RewiringSensitivity
    ];

    public static readonly string[] Serendipity =
    [
        ExplorationTendency,
        SerendipitySensitivity,
        KnowledgeRecombinationRate,
        SerendipityThreshold
    ];

    public static readonly string[] TrustDynamics =
    [
        TrustGrowthRate,
        TrustDecayRate,
        TrustSaturationStrength,
        TrustCapacity,
        TrustCapacityPenalty,
        DistrustPenalty,
        ConstructiveCriticismBonus
    ];

    public static readonly string[] Challenge =
    [
        ChallengeLevel,
        RequiredKnowledgeDiversity,
        RequiredCrossDomainExposure,
        RequiredRewiringScore
    ];

    public static readonly string[] All =
    [
        ..BoundaryConditions,
        ..KnowledgeAndRewiring,
        ..Serendipity,
        ..TrustDynamics,
        ..Challenge
    ];

    public static readonly (string Label, string[] Parameters)[] Groups =
    [
        ("境界条件", BoundaryConditions),
        ("知識・再配線", KnowledgeAndRewiring),
        ("セレンディピティ", Serendipity),
        ("信頼ダイナミクス", TrustDynamics),
        ("Challenge", Challenge)
    ];

    public static string GetLabel(string parameterName) => parameterName switch
    {
        InformationSharingLevel => "情報共有",
        CooperationLevel => "協調性",
        CompetitionLevel => "競争性",
        PsychologicalSafetyLevel => "心理的安全性",
        LearningOrientationLevel => "学習志向",
        CustomerOrientationLevel => "顧客志向",
        ShortTermResultPressureLevel => "短期成果圧力",
        EffectiveTrustThreshold => "実効信頼閾値",
        KnowledgeStock => "知識蓄積",
        KnowledgeDiversity => "知識多様性",
        ExternalShockLevel => "外部刺激レベル",
        CrossDomainExposure => "異分野接触度",
        RewiringSensitivity => "再配線感度",
        ExplorationTendency => "探索傾向",
        SerendipitySensitivity => "セレンディピティ感受性",
        KnowledgeRecombinationRate => "知識再結合率",
        SerendipityThreshold => "セレンディピティ閾値",
        TrustGrowthRate => "信頼成長率",
        TrustDecayRate => "信頼自然減衰率",
        TrustSaturationStrength => "信頼飽和強度",
        TrustCapacity => "信頼容量",
        TrustCapacityPenalty => "信頼容量ペナルティ",
        DistrustPenalty => "不信ペナルティ",
        ConstructiveCriticismBonus => "建設的批判ボーナス",
        ChallengeLevel => "Challengeレベル",
        RequiredKnowledgeDiversity => "必要知識多様性",
        RequiredCrossDomainExposure => "必要異分野接触度",
        RequiredRewiringScore => "必要再配線スコア",
        _ => parameterName
    };

    public static bool IsSupported(string parameterName)
    {
        return All.Contains(parameterName, StringComparer.Ordinal);
    }

    public static bool TryApply(Experiment experiment, string parameterName, double value)
    {
        switch (parameterName)
        {
            case InformationSharingLevel:
                experiment.InformationSharingLevel = value;
                return true;
            case CooperationLevel:
                experiment.CooperationLevel = value;
                return true;
            case CompetitionLevel:
                experiment.CompetitionLevel = value;
                return true;
            case PsychologicalSafetyLevel:
                experiment.PsychologicalSafetyLevel = value;
                return true;
            case LearningOrientationLevel:
                experiment.LearningOrientationLevel = value;
                return true;
            case CustomerOrientationLevel:
                experiment.CustomerOrientationLevel = value;
                return true;
            case ShortTermResultPressureLevel:
                experiment.ShortTermResultPressureLevel = value;
                return true;
            case EffectiveTrustThreshold:
                experiment.EffectiveTrustThreshold = value;
                return true;
            case KnowledgeStock:
                experiment.KnowledgeStock = value;
                return true;
            case KnowledgeDiversity:
                experiment.KnowledgeDiversity = value;
                return true;
            case ExternalShockLevel:
                experiment.ExternalShockLevel = value;
                return true;
            case CrossDomainExposure:
                experiment.CrossDomainExposure = value;
                return true;
            case RewiringSensitivity:
                experiment.RewiringSensitivity = value;
                return true;
            case ExplorationTendency:
                experiment.ExplorationTendency = value;
                return true;
            case SerendipitySensitivity:
                experiment.SerendipitySensitivity = value;
                return true;
            case KnowledgeRecombinationRate:
                experiment.KnowledgeRecombinationRate = value;
                return true;
            case SerendipityThreshold:
                experiment.SerendipityThreshold = value;
                return true;
            case TrustGrowthRate:
                experiment.TrustGrowthRate = value;
                return true;
            case TrustDecayRate:
                experiment.TrustDecayRate = value;
                return true;
            case TrustSaturationStrength:
                experiment.TrustSaturationStrength = value;
                return true;
            case TrustCapacity:
                experiment.TrustCapacity = Math.Max(1, (int)Math.Round(value));
                return true;
            case TrustCapacityPenalty:
                experiment.TrustCapacityPenalty = value;
                return true;
            case DistrustPenalty:
                experiment.DistrustPenalty = value;
                return true;
            case ConstructiveCriticismBonus:
                experiment.ConstructiveCriticismBonus = value;
                return true;
            case ChallengeLevel:
                experiment.ChallengeLevel = value;
                return true;
            case RequiredKnowledgeDiversity:
                experiment.RequiredKnowledgeDiversity = value;
                return true;
            case RequiredCrossDomainExposure:
                experiment.RequiredCrossDomainExposure = value;
                return true;
            case RequiredRewiringScore:
                experiment.RequiredRewiringScore = value;
                return true;
            default:
                return false;
        }
    }

    public static bool TryApply(Scenario scenario, string parameterName, double value)
    {
        switch (parameterName)
        {
            case InformationSharingLevel:
                scenario.InformationSharingLevel = value;
                return true;
            case CooperationLevel:
                scenario.CooperationLevel = value;
                return true;
            case CompetitionLevel:
                scenario.CompetitionLevel = value;
                return true;
            case PsychologicalSafetyLevel:
                scenario.PsychologicalSafetyLevel = value;
                return true;
            case LearningOrientationLevel:
                scenario.LearningOrientationLevel = value;
                return true;
            case CustomerOrientationLevel:
                scenario.CustomerOrientationLevel = value;
                return true;
            case ShortTermResultPressureLevel:
                scenario.ShortTermResultPressureLevel = value;
                return true;
            case EffectiveTrustThreshold:
                scenario.EffectiveTrustThreshold = value;
                return true;
            case KnowledgeStock:
                scenario.KnowledgeStock = value;
                return true;
            case KnowledgeDiversity:
                scenario.KnowledgeDiversity = value;
                return true;
            case ExternalShockLevel:
                scenario.ExternalShockLevel = value;
                return true;
            case CrossDomainExposure:
                scenario.CrossDomainExposure = value;
                return true;
            case RewiringSensitivity:
                scenario.RewiringSensitivity = value;
                return true;
            case ExplorationTendency:
                scenario.ExplorationTendency = value;
                return true;
            case SerendipitySensitivity:
                scenario.SerendipitySensitivity = value;
                return true;
            case KnowledgeRecombinationRate:
                scenario.KnowledgeRecombinationRate = value;
                return true;
            case SerendipityThreshold:
                scenario.SerendipityThreshold = value;
                return true;
            case TrustGrowthRate:
                scenario.TrustGrowthRate = value;
                return true;
            case TrustDecayRate:
                scenario.TrustDecayRate = value;
                return true;
            case TrustSaturationStrength:
                scenario.TrustSaturationStrength = value;
                return true;
            case TrustCapacity:
                scenario.TrustCapacity = Math.Max(1, (int)Math.Round(value));
                return true;
            case TrustCapacityPenalty:
                scenario.TrustCapacityPenalty = value;
                return true;
            case DistrustPenalty:
                scenario.DistrustPenalty = value;
                return true;
            case ConstructiveCriticismBonus:
                scenario.ConstructiveCriticismBonus = value;
                return true;
            case ChallengeLevel:
                scenario.ChallengeLevel = value;
                return true;
            case RequiredKnowledgeDiversity:
                scenario.RequiredKnowledgeDiversity = value;
                return true;
            case RequiredCrossDomainExposure:
                scenario.RequiredCrossDomainExposure = value;
                return true;
            case RequiredRewiringScore:
                scenario.RequiredRewiringScore = value;
                return true;
            default:
                return false;
        }
    }

    public static bool TryApply(SimulationProject simulationProject, string parameterName, double value)
    {
        switch (parameterName)
        {
            case InformationSharingLevel:
                simulationProject.InformationSharingLevel = value;
                return true;
            case CooperationLevel:
                simulationProject.CooperationLevel = value;
                return true;
            case CompetitionLevel:
                simulationProject.CompetitionLevel = value;
                return true;
            case PsychologicalSafetyLevel:
                simulationProject.PsychologicalSafetyLevel = value;
                return true;
            case LearningOrientationLevel:
                simulationProject.LearningOrientationLevel = value;
                return true;
            case CustomerOrientationLevel:
                simulationProject.CustomerOrientationLevel = value;
                return true;
            case ShortTermResultPressureLevel:
                simulationProject.ShortTermResultPressureLevel = value;
                return true;
            case EffectiveTrustThreshold:
                simulationProject.EffectiveTrustThreshold = value;
                return true;
            case KnowledgeStock:
                simulationProject.KnowledgeStock = value;
                return true;
            case KnowledgeDiversity:
                simulationProject.KnowledgeDiversity = value;
                return true;
            case ExternalShockLevel:
                simulationProject.ExternalShockLevel = value;
                return true;
            case CrossDomainExposure:
                simulationProject.CrossDomainExposure = value;
                return true;
            case RewiringSensitivity:
                simulationProject.RewiringSensitivity = value;
                return true;
            case ExplorationTendency:
                simulationProject.ExplorationTendency = value;
                return true;
            case SerendipitySensitivity:
                simulationProject.SerendipitySensitivity = value;
                return true;
            case KnowledgeRecombinationRate:
                simulationProject.KnowledgeRecombinationRate = value;
                return true;
            case SerendipityThreshold:
                simulationProject.SerendipityThreshold = value;
                return true;
            case TrustGrowthRate:
                simulationProject.TrustGrowthRate = value;
                return true;
            case TrustDecayRate:
                simulationProject.TrustDecayRate = value;
                return true;
            case TrustSaturationStrength:
                simulationProject.TrustSaturationStrength = value;
                return true;
            case TrustCapacity:
                simulationProject.TrustCapacity = Math.Max(1, (int)Math.Round(value));
                return true;
            case TrustCapacityPenalty:
                simulationProject.TrustCapacityPenalty = value;
                return true;
            case DistrustPenalty:
                simulationProject.DistrustPenalty = value;
                return true;
            case ConstructiveCriticismBonus:
                simulationProject.ConstructiveCriticismBonus = value;
                return true;
            case ChallengeLevel:
                simulationProject.ChallengeLevel = value;
                return true;
            case RequiredKnowledgeDiversity:
                simulationProject.RequiredKnowledgeDiversity = value;
                return true;
            case RequiredCrossDomainExposure:
                simulationProject.RequiredCrossDomainExposure = value;
                return true;
            case RequiredRewiringScore:
                simulationProject.RequiredRewiringScore = value;
                return true;
            default:
                return false;
        }
    }
}

public static class ParameterSweepStatus
{
    public const string Created = "Created";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string StopRequested = "StopRequested";
    public const string Stopped = "Stopped";
}

public static class PhaseDiagramStatus
{
    public const string Created = "Created";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class SimulationPhase
{
    public const string Forming = "Forming";
    public const string Learning = "Learning";
    public const string Adaptation = "Adaptation";
    public const string Emergent = "Emergent";
    public const string Silo = "Silo";
    public const string Chaos = "Chaos";
    public const string Collapse = "Collapse";
    public const string Stable = "Stable";
}
