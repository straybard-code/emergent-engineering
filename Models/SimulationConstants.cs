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

public static class ThanksCoinDefaults
{
    public const bool EnableThanksCoin = false;
    public const double ThanksCoinRate = 0.20;
    public const double ThanksCoinRespectGain = 0.03;
    public const double ThanksCoinTrustGain = 0.01;
    public const double ThanksCoinReconfigurationGain = 0.03;
    public const double ThanksCoinPsychologicalSafetyGain = 0.02;
    public const double ThanksCoinBridgeGain = 0.03;
    public const double ThanksCoinPopularityBias = 0.30;
    public const double ThanksCoinDiversityBonus = 0.30;
    public const double ThanksCoinChallengeBonus = 0.20;
}

public static class MutualRespectDefaults
{
    public const double Base = 0.30;
    public const double GrowthRate = 0.04;
    public const double DecayRate = 0.002;
    public const double DiversitySensitivity = 0.40;
    public const double ChallengeSensitivity = 0.40;
    public const double BridgeSensitivity = 0.40;
    public const double PopularityPenalty = 0.30;
}

public static class ThanksCoinTypes
{
    public const string Help = "Help";
    public const string Idea = "Idea";
    public const string Challenge = "Challenge";
    public const string Bridge = "Bridge";

    public static readonly string[] All =
    [
        Help,
        Idea,
        Challenge,
        Bridge
    ];
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

    public const string ThanksCoinRate = "ThanksCoinRate";
    public const string ThanksCoinRespectGain = "ThanksCoinRespectGain";
    public const string ThanksCoinTrustGain = "ThanksCoinTrustGain";
    public const string ThanksCoinReconfigurationGain = "ThanksCoinReconfigurationGain";
    public const string ThanksCoinPsychologicalSafetyGain = "ThanksCoinPsychologicalSafetyGain";
    public const string ThanksCoinBridgeGain = "ThanksCoinBridgeGain";
    public const string ThanksCoinPopularityBias = "ThanksCoinPopularityBias";
    public const string ThanksCoinDiversityBonus = "ThanksCoinDiversityBonus";
    public const string ThanksCoinChallengeBonus = "ThanksCoinChallengeBonus";

    public const string MutualRespectBase = "MutualRespectBase";
    public const string MutualRespectGrowthRate = "MutualRespectGrowthRate";
    public const string MutualRespectDecayRate = "MutualRespectDecayRate";
    public const string MutualRespectDiversitySensitivity = "MutualRespectDiversitySensitivity";
    public const string MutualRespectChallengeSensitivity = "MutualRespectChallengeSensitivity";
    public const string MutualRespectBridgeSensitivity = "MutualRespectBridgeSensitivity";
    public const string MutualRespectPopularityPenalty = "MutualRespectPopularityPenalty";

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

    public static readonly string[] ThanksCoin =
    [
        ThanksCoinRate,
        ThanksCoinRespectGain,
        ThanksCoinTrustGain,
        ThanksCoinReconfigurationGain,
        ThanksCoinPsychologicalSafetyGain,
        ThanksCoinBridgeGain,
        ThanksCoinPopularityBias,
        ThanksCoinDiversityBonus,
        ThanksCoinChallengeBonus
    ];

    public static readonly string[] MutualRespect =
    [
        MutualRespectBase,
        MutualRespectGrowthRate,
        MutualRespectDecayRate,
        MutualRespectDiversitySensitivity,
        MutualRespectChallengeSensitivity,
        MutualRespectBridgeSensitivity,
        MutualRespectPopularityPenalty
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
        ..ThanksCoin,
        ..MutualRespect,
        ..Challenge
    ];

    public static readonly (string Label, string[] Parameters)[] Groups =
    [
        ("境界条件", BoundaryConditions),
        ("知識・再配線", KnowledgeAndRewiring),
        ("セレンディピティ", Serendipity),
        ("信頼ダイナミクス", TrustDynamics),
        ("Thanks Coin / 相互敬意", ThanksCoin),
        ("相互敬意", MutualRespect),
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
        ThanksCoinRate => "Thanks Coin発生率",
        ThanksCoinRespectGain => "敬意増加量",
        ThanksCoinTrustGain => "信頼増加量",
        ThanksCoinReconfigurationGain => "知識再構成増加量",
        ThanksCoinPsychologicalSafetyGain => "心理的安全性増加量",
        ThanksCoinBridgeGain => "橋渡し増加量",
        ThanksCoinPopularityBias => "人気集中バイアス",
        ThanksCoinDiversityBonus => "多様性ボーナス",
        ThanksCoinChallengeBonus => "建設的異論ボーナス",
        MutualRespectBase => "相互敬意ベース",
        MutualRespectGrowthRate => "相互敬意成長率",
        MutualRespectDecayRate => "相互敬意減衰率",
        MutualRespectDiversitySensitivity => "異質性尊重感度",
        MutualRespectChallengeSensitivity => "異論尊重感度",
        MutualRespectBridgeSensitivity => "橋渡し尊重感度",
        MutualRespectPopularityPenalty => "人気集中ペナルティ",
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
            case ThanksCoinRate:
                experiment.ThanksCoinRate = value;
                return true;
            case ThanksCoinRespectGain:
                experiment.ThanksCoinRespectGain = value;
                return true;
            case ThanksCoinTrustGain:
                experiment.ThanksCoinTrustGain = value;
                return true;
            case ThanksCoinReconfigurationGain:
                experiment.ThanksCoinReconfigurationGain = value;
                return true;
            case ThanksCoinPsychologicalSafetyGain:
                experiment.ThanksCoinPsychologicalSafetyGain = value;
                return true;
            case ThanksCoinBridgeGain:
                experiment.ThanksCoinBridgeGain = value;
                return true;
            case ThanksCoinPopularityBias:
                experiment.ThanksCoinPopularityBias = value;
                return true;
            case ThanksCoinDiversityBonus:
                experiment.ThanksCoinDiversityBonus = value;
                return true;
            case ThanksCoinChallengeBonus:
                experiment.ThanksCoinChallengeBonus = value;
                return true;
            case MutualRespectBase:
                experiment.MutualRespectBase = value;
                return true;
            case MutualRespectGrowthRate:
                experiment.MutualRespectGrowthRate = value;
                return true;
            case MutualRespectDecayRate:
                experiment.MutualRespectDecayRate = value;
                return true;
            case MutualRespectDiversitySensitivity:
                experiment.MutualRespectDiversitySensitivity = value;
                return true;
            case MutualRespectChallengeSensitivity:
                experiment.MutualRespectChallengeSensitivity = value;
                return true;
            case MutualRespectBridgeSensitivity:
                experiment.MutualRespectBridgeSensitivity = value;
                return true;
            case MutualRespectPopularityPenalty:
                experiment.MutualRespectPopularityPenalty = value;
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
            case ThanksCoinRate:
                scenario.ThanksCoinRate = value;
                return true;
            case ThanksCoinRespectGain:
                scenario.ThanksCoinRespectGain = value;
                return true;
            case ThanksCoinTrustGain:
                scenario.ThanksCoinTrustGain = value;
                return true;
            case ThanksCoinReconfigurationGain:
                scenario.ThanksCoinReconfigurationGain = value;
                return true;
            case ThanksCoinPsychologicalSafetyGain:
                scenario.ThanksCoinPsychologicalSafetyGain = value;
                return true;
            case ThanksCoinBridgeGain:
                scenario.ThanksCoinBridgeGain = value;
                return true;
            case ThanksCoinPopularityBias:
                scenario.ThanksCoinPopularityBias = value;
                return true;
            case ThanksCoinDiversityBonus:
                scenario.ThanksCoinDiversityBonus = value;
                return true;
            case ThanksCoinChallengeBonus:
                scenario.ThanksCoinChallengeBonus = value;
                return true;
            case MutualRespectBase:
                scenario.MutualRespectBase = value;
                return true;
            case MutualRespectGrowthRate:
                scenario.MutualRespectGrowthRate = value;
                return true;
            case MutualRespectDecayRate:
                scenario.MutualRespectDecayRate = value;
                return true;
            case MutualRespectDiversitySensitivity:
                scenario.MutualRespectDiversitySensitivity = value;
                return true;
            case MutualRespectChallengeSensitivity:
                scenario.MutualRespectChallengeSensitivity = value;
                return true;
            case MutualRespectBridgeSensitivity:
                scenario.MutualRespectBridgeSensitivity = value;
                return true;
            case MutualRespectPopularityPenalty:
                scenario.MutualRespectPopularityPenalty = value;
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
            case ThanksCoinRate:
                simulationProject.ThanksCoinRate = value;
                return true;
            case ThanksCoinRespectGain:
                simulationProject.ThanksCoinRespectGain = value;
                return true;
            case ThanksCoinTrustGain:
                simulationProject.ThanksCoinTrustGain = value;
                return true;
            case ThanksCoinReconfigurationGain:
                simulationProject.ThanksCoinReconfigurationGain = value;
                return true;
            case ThanksCoinPsychologicalSafetyGain:
                simulationProject.ThanksCoinPsychologicalSafetyGain = value;
                return true;
            case ThanksCoinBridgeGain:
                simulationProject.ThanksCoinBridgeGain = value;
                return true;
            case ThanksCoinPopularityBias:
                simulationProject.ThanksCoinPopularityBias = value;
                return true;
            case ThanksCoinDiversityBonus:
                simulationProject.ThanksCoinDiversityBonus = value;
                return true;
            case ThanksCoinChallengeBonus:
                simulationProject.ThanksCoinChallengeBonus = value;
                return true;
            case MutualRespectBase:
                simulationProject.MutualRespectBase = value;
                return true;
            case MutualRespectGrowthRate:
                simulationProject.MutualRespectGrowthRate = value;
                return true;
            case MutualRespectDecayRate:
                simulationProject.MutualRespectDecayRate = value;
                return true;
            case MutualRespectDiversitySensitivity:
                simulationProject.MutualRespectDiversitySensitivity = value;
                return true;
            case MutualRespectChallengeSensitivity:
                simulationProject.MutualRespectChallengeSensitivity = value;
                return true;
            case MutualRespectBridgeSensitivity:
                simulationProject.MutualRespectBridgeSensitivity = value;
                return true;
            case MutualRespectPopularityPenalty:
                simulationProject.MutualRespectPopularityPenalty = value;
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
