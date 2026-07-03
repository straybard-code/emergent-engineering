namespace EmergentEngineering.Pages.Experiments;

public sealed class ExperimentActionTimelinePoint
{
    public int StepNo { get; init; }
    public int RunCount { get; init; }
    public double ShareInfoRate { get; init; }
    public double AskHelpRate { get; init; }
    public double ProposeIdeaRate { get; init; }
    public double CriticizeRate { get; init; }
    public double WorkAloneRate { get; init; }
    public double SupportOtherRate { get; init; }
    public double WaitRate { get; init; }
    public double OtherRate { get; init; }
}
