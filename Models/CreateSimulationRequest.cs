using System.ComponentModel.DataAnnotations;

namespace EmergentEngineering.Models;

public sealed class CreateSimulationRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Required]
    public string Purpose { get; set; } = "";

    [Required]
    public string BoundaryConditions { get; set; } = "";

    [Required]
    public string KpiDefinition { get; set; } = "";

    [Range(1, 20)]
    public int AgentCount { get; set; } = 4;

    [Range(1, 100)]
    public int TotalSteps { get; set; } = 5;
}
