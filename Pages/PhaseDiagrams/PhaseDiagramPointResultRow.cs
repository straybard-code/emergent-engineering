namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class PhaseDiagramPointResultRow
{
    public double XValue { get; set; }
    public double YValue { get; set; }
    public int? ExperimentId { get; set; }
    public string DominantPhase { get; set; } = "-";
    public double EmergentRate { get; set; }
    public double StableRate { get; set; }
    public double LearningRate { get; set; }
    public double SiloRate { get; set; }
    public double AverageTrust { get; set; }
    public double AverageEffectiveDensity { get; set; }
    public double AverageKnowledgeDiversity { get; set; }
    public double AverageKnowledgeReconfigurationScore { get; set; }
    public double AverageSerendipityRate { get; set; }
    public double AveragePipelineCompletionScore { get; set; }
    public string DominantBottleneck { get; set; } = "--";
}
