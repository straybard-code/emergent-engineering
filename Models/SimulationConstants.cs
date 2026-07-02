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
