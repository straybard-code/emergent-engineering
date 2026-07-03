namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class ParameterSweepPhaseRow
{
    public double ParameterValue { get; set; }
    public Dictionary<string, int> Counts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public int GetCount(string phase)
    {
        return Counts.TryGetValue(phase, out var count) ? count : 0;
    }
}
