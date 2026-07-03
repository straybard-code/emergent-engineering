namespace EmergentEngineering.Pages.Simulations;

public sealed class NetworkSnapshot
{
    public int StepNo { get; set; }
    public string Phase { get; set; } = "";
    public double AverageTrust { get; set; }
    public double AverageAbsTrust { get; set; }
    public string HubAgentName { get; set; } = "-";
    public int IsolatedCount { get; set; }
    public List<NetworkNode> Nodes { get; set; } = [];
    public List<NetworkEdge> Edges { get; set; } = [];
}
