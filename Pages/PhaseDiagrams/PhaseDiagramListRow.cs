namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class PhaseDiagramListRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ScenarioName { get; set; } = "-";
    public string XAxisLabel { get; set; } = "-";
    public string YAxisLabel { get; set; } = "-";
    public int GridPointCount { get; set; }
    public bool IsAdaptiveSweep { get; set; }
    public int? ParentPhaseDiagramId { get; set; }
    public string ParentPhaseDiagramName { get; set; } = "-";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
