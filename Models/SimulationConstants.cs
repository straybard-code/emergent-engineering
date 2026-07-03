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
}

public static class ExperimentStatus
{
    public const string Created = "Created";
    public const string Running = "Running";
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

public static class BoundaryParameterNames
{
    public const string InformationSharingLevel = "InformationSharingLevel";
    public const string CooperationLevel = "CooperationLevel";
    public const string CompetitionLevel = "CompetitionLevel";
    public const string PsychologicalSafetyLevel = "PsychologicalSafetyLevel";
    public const string LearningOrientationLevel = "LearningOrientationLevel";
    public const string CustomerOrientationLevel = "CustomerOrientationLevel";
    public const string ShortTermResultPressureLevel = "ShortTermResultPressureLevel";

    public static readonly string[] All =
    [
        InformationSharingLevel,
        CooperationLevel,
        CompetitionLevel,
        PsychologicalSafetyLevel,
        LearningOrientationLevel,
        CustomerOrientationLevel,
        ShortTermResultPressureLevel
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

public static class SimulationPhase
{
    public const string Forming = "Forming";
    public const string Learning = "Learning";
    public const string Emergent = "Emergent";
    public const string Silo = "Silo";
    public const string Chaos = "Chaos";
    public const string Collapse = "Collapse";
    public const string Stable = "Stable";
}
