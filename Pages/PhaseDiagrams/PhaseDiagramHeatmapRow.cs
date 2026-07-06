namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class PhaseDiagramHeatmapRow
{
    public double YValue { get; set; }
    public List<PhaseDiagramHeatmapCell> Cells { get; set; } = [];
}
