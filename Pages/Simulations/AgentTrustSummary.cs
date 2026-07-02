namespace EmergentEngineering.Pages.Simulations;

public sealed class AgentTrustSummary
{
    public string AgentName { get; set; } = "";
    public double AverageTrust { get; set; }
    public string TrustJson { get; set; } = "{}";
}
