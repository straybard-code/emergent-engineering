using System.ComponentModel.DataAnnotations;

namespace EmergentEngineering.Models;

public sealed class PhaseDiagram
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int BaseScenarioId { get; set; }

    [Required]
    [StringLength(120)]
    public string XParameterName { get; set; } = BoundaryParameterNames.TrustGrowthRate;

    [Required]
    [StringLength(160)]
    public string XParameterDisplayName { get; set; } = BoundaryParameterNames.GetLabel(BoundaryParameterNames.TrustGrowthRate);

    [Range(0, 20)]
    public double XStartValue { get; set; } = 0.2;

    [Range(0, 20)]
    public double XEndValue { get; set; } = 0.8;

    [Range(typeof(double), "0.001", "20")]
    public double XStepValue { get; set; } = 0.1;

    [Required]
    [StringLength(120)]
    public string YParameterName { get; set; } = BoundaryParameterNames.KnowledgeDiversity;

    [Required]
    [StringLength(160)]
    public string YParameterDisplayName { get; set; } = BoundaryParameterNames.GetLabel(BoundaryParameterNames.KnowledgeDiversity);

    [Range(0, 20)]
    public double YStartValue { get; set; } = 0.3;

    [Range(0, 20)]
    public double YEndValue { get; set; } = 1.0;

    [Range(typeof(double), "0.001", "20")]
    public double YStepValue { get; set; } = 0.1;

    [Range(1, 100)]
    public int RunsPerPoint { get; set; } = 3;

    [Range(2, 100)]
    public int AgentCount { get; set; } = 10;

    [Range(1, 5000)]
    public int TotalSteps { get; set; } = 50;

    public int? ParentPhaseDiagramId { get; set; }

    [StringLength(40)]
    public string AdaptiveSourceType { get; set; } = "";

    [StringLength(1000)]
    public string AdaptiveReason { get; set; } = "";

    public bool IsAdaptiveSweep { get; set; }

    [Required]
    [StringLength(40)]
    public string LlmProvider { get; set; } = LlmDefaults.Provider;

    [Required]
    [StringLength(120)]
    public string LlmModel { get; set; } = LlmDefaults.MockModel;

    [Required]
    [StringLength(40)]
    public string Status { get; set; } = PhaseDiagramStatus.Created;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Scenario? BaseScenario { get; set; }
    public PhaseDiagram? ParentPhaseDiagram { get; set; }
    public List<PhaseDiagramPoint> Points { get; set; } = [];
}
