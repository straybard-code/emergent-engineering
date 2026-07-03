namespace EmergentEngineering.Pages.Simulations;

public sealed class NetworkNode
{
    public int AgentId { get; set; }
    public string AgentName { get; set; } = "";
    public string Role { get; set; } = "";
    public string Personality { get; set; } = "";
    public string Orientation { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsHub { get; set; }
}
