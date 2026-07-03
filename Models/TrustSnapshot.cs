namespace EmergentEngineering.Models;

public sealed class TrustSnapshot
{
    public int Id { get; set; }
    public int SimulationProjectId { get; set; }
    public SimulationProject? SimulationProject { get; set; }
    public int StepNo { get; set; }
    public int SourceAgentId { get; set; }
    public int TargetAgentId { get; set; }
    public string SourceAgentName { get; set; } = "";
    public string TargetAgentName { get; set; } = "";
    public double TrustValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
