namespace EmergentEngineering.Pages.Simulations;

public sealed class NetworkEdge
{
    public int SourceAgentId { get; set; }
    public int TargetAgentId { get; set; }
    public string SourceAgentName { get; set; } = "";
    public string TargetAgentName { get; set; } = "";
    public double Trust { get; set; }
    public double StrokeWidth { get; set; }
    public string StrokeColor { get; set; } = "#8bb8ef";
}
