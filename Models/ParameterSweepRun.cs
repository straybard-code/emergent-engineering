using System.ComponentModel.DataAnnotations;

namespace EmergentEngineering.Models;

public sealed class ParameterSweepRun
{
    public int Id { get; set; }
    public int ParameterSweepId { get; set; }
    public ParameterSweep? ParameterSweep { get; set; }
    public double ParameterValue { get; set; }
    public int ExperimentId { get; set; }
    public int RunCount { get; set; }

    [Required]
    public string FinalPhaseSummaryJson { get; set; } = "{}";

    public double AverageTrust { get; set; }
    public double AverageAbsTrust { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
