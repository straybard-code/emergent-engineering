namespace EmergentEngineering.Pages.Experiments;

public sealed class TrustStatePoint
{
    public int StepNo { get; set; }
    public double AverageTrust { get; set; }
    public double AverageAbsTrust { get; set; }
    public int Count { get; set; }
}
