namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class PhaseDiagramRegionSummary
{
    public string Title { get; set; } = "";
    public string EmptyMessage { get; set; } = "";
    public int Count { get; set; }
    public double? XMin { get; set; }
    public double? XMax { get; set; }
    public double? YMin { get; set; }
    public double? YMax { get; set; }
    public double? AverageX { get; set; }
    public double? AverageY { get; set; }
    public double? AveragePipelineCompletionScore { get; set; }
    public string MostCommonBottleneck { get; set; } = "--";
    public string Interpretation { get; set; } = "";

    public bool HasData => Count > 0;
}
