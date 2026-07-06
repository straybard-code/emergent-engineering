namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class PhaseDiagramHeatmapCell
{
    public double XValue { get; set; }
    public double YValue { get; set; }
    public int? ExperimentId { get; set; }
    public string Text { get; set; } = "--";
    public string CssClass { get; set; } = "";
    public bool IsBoundary { get; set; }
    public string? Tooltip { get; set; }
}
