namespace EmergentEngineering.Models;

public sealed class Agent
{
    public int Id { get; set; }
    public int SimulationProjectId { get; set; }
    public SimulationProject? SimulationProject { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Memory { get; set; } = "";
    public string TrustJson { get; set; } = "{}";
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public List<AgentAction> Actions { get; set; } = [];
}
