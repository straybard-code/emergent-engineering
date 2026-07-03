namespace EmergentEngineering.Pages.Simulations;

public sealed class ActionTimelinePoint
{
    public int StepNo { get; init; }
    public int TotalCount { get; init; }
    public int ShareInfoCount { get; init; }
    public int AskHelpCount { get; init; }
    public int ProposeIdeaCount { get; init; }
    public int CriticizeCount { get; init; }
    public int WorkAloneCount { get; init; }
    public int SupportOtherCount { get; init; }
    public int WaitCount { get; init; }
    public int OtherCount { get; init; }
    public double ShareInfoRate { get; init; }
    public double AskHelpRate { get; init; }
    public double ProposeIdeaRate { get; init; }
    public double CriticizeRate { get; init; }
    public double WorkAloneRate { get; init; }
    public double SupportOtherRate { get; init; }
    public double WaitRate { get; init; }
    public double OtherRate { get; init; }
    public string Phase { get; init; } = "";
    public bool PhaseChanged { get; set; }
}
