namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class ParameterSweepListRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ScenarioName { get; set; } = "";
    public string TargetParameter { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
