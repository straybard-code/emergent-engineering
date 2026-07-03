namespace EmergentEngineering.Models;

public sealed class ThresholdSweepPoint
{
    public double Threshold { get; set; }
    public double EffectiveNetworkDensity { get; set; }
    public int StrongLinkCount { get; set; }
    public int WeakLinkCount { get; set; }
    public int ComponentCount { get; set; }
    public int IsolatedCount { get; set; }
}
