namespace EmergentEngineering.Pages.Simulations;

public sealed class TrustChangeLogRow
{
    public int StepNo { get; set; }
    public string AgentName { get; set; } = "";
    public string Action { get; set; } = "";
    public double? TrustBefore { get; set; }
    public double? TrustDelta { get; set; }
    public double? TrustAfter { get; set; }
    public string Phase { get; set; } = "";
}
