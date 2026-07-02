namespace EmergentEngineering.Models;

public sealed class AgentAction
{
    public int Id { get; set; }
    public int SimulationProjectId { get; set; }
    public int StepNo { get; set; }
    public int AgentId { get; set; }
    public Agent? Agent { get; set; }
    public string Message { get; set; } = "";
    public string Memory { get; set; } = "";
    public string Action { get; set; } = AgentActionType.Wait;
    public string TargetAgentName { get; set; } = "";
    public string RawLlmResponse { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
