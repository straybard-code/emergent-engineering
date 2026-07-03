namespace EmergentEngineering.Pages.Experiments;

public sealed class ExperimentThresholdSweepPoint
{
    public double Threshold { get; set; }
    public double AverageEffectiveNetworkDensity { get; set; }
    public double AverageStrongLinkCount { get; set; }
    public double AverageWeakLinkCount { get; set; }
    public double AverageComponentCount { get; set; }
    public double AverageIsolatedCount { get; set; }
    public int RunCount { get; set; }
}
