namespace EmergentEngineering.Pages.Simulations;

public sealed class AgentTrustSummary
{
    public string AgentName { get; set; } = "";
    public string Role { get; set; } = "";
    public string Personality { get; set; } = "";
    public string Orientation { get; set; } = "";
    public double AverageTrust { get; set; }
    public string TrustJson { get; set; } = "{}";
}
