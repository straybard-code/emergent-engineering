namespace EmergentEngineering.Models;

public sealed class SimulationStep
{
    public int Id { get; set; }
    public int SimulationProjectId { get; set; }
    public SimulationProject? SimulationProject { get; set; }
    public int StepNo { get; set; }
    public string Phase { get; set; } = SimulationPhase.Forming;
    public string StateJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
