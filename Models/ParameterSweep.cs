using System.ComponentModel.DataAnnotations;

namespace EmergentEngineering.Models;

public sealed class ParameterSweep
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int ScenarioId { get; set; }

    [Required]
    [StringLength(120)]
    public string TargetParameter { get; set; } = BoundaryParameterNames.InformationSharingLevel;

    [Range(0, 1)]
    public double StartValue { get; set; } = 0;

    [Range(0, 1)]
    public double EndValue { get; set; } = 1;

    [Range(typeof(double), "0.01", "1")]
    public double StepValue { get; set; } = 0.1;

    [Range(1, 50)]
    public int RunCountPerValue { get; set; } = 3;

    [Range(2, 50)]
    public int AgentCount { get; set; } = 4;

    [Range(1, 200)]
    public int TotalSteps { get; set; } = 5;

    [Required]
    [StringLength(40)]
    public string LlmProvider { get; set; } = LlmDefaults.Provider;

    [Required]
    [StringLength(120)]
    public string LlmModel { get; set; } = LlmDefaults.MockModel;

    [Required]
    [StringLength(40)]
    public string Status { get; set; } = ParameterSweepStatus.Created;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public List<ParameterSweepRun> Runs { get; set; } = [];
}
