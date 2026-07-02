namespace EmergentEngineering.Models;

public sealed class SimulationProject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string BoundaryConditions { get; set; } = "";
    public string KpiDefinition { get; set; } = "";
    public int AgentCount { get; set; }
    public int TotalSteps { get; set; }
    public int CurrentStep { get; set; }
    public string Status { get; set; } = SimulationStatus.Created;
    public string Phase { get; set; } = SimulationPhase.Forming;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Agent> Agents { get; set; } = [];
    public List<SimulationStep> Steps { get; set; } = [];
}
