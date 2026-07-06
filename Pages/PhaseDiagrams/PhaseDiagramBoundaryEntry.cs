namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class PhaseDiagramBoundaryEntry
{
    public string FromPhase { get; set; } = "--";
    public string ToPhase { get; set; } = "--";
    public double XValue { get; set; }
    public double YValue { get; set; }
    public double NeighborXValue { get; set; }
    public double NeighborYValue { get; set; }
    public string Direction { get; set; } = "";
}
