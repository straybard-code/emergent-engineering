using System.ComponentModel.DataAnnotations;

namespace EmergentEngineering.Models;

public sealed class PhaseDiagramPoint
{
    public int Id { get; set; }
    public int PhaseDiagramId { get; set; }
    public PhaseDiagram? PhaseDiagram { get; set; }
    public double XValue { get; set; }
    public double YValue { get; set; }
    public int? ExperimentId { get; set; }
    public Experiment? Experiment { get; set; }

    [Required]
    public string FinalPhaseSummary { get; set; } = "{}";

    [Required]
    [StringLength(40)]
    public string DominantPhase { get; set; } = SimulationPhase.Forming;

    public double EmergentRate { get; set; }
    public double StableRate { get; set; }
    public double LearningRate { get; set; }
    public double SiloRate { get; set; }
    public double ChaosRate { get; set; }
    public double CollapseRate { get; set; }
    public double AverageTrust { get; set; }
    public double AverageEffectiveDensity { get; set; }
    public double AverageKnowledgeDiversity { get; set; }
    public double AverageKnowledgeRecombinationScore { get; set; }
    public double AverageKnowledgeReconfigurationScore { get; set; }
    public double AverageSerendipityRate { get; set; }
    public double AveragePipelineCompletionScore { get; set; }

    [Required]
    [StringLength(80)]
    public string DominantBottleneck { get; set; } = "--";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
