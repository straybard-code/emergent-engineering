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

    public double EffectiveNetworkDensity
    {
        get => AverageEffectiveNetworkDensity;
        set => AverageEffectiveNetworkDensity = value;
    }

    public double StrongLinkCount
    {
        get => AverageStrongLinkCount;
        set => AverageStrongLinkCount = value;
    }

    public double WeakLinkCount
    {
        get => AverageWeakLinkCount;
        set => AverageWeakLinkCount = value;
    }

    public double ComponentCount
    {
        get => AverageComponentCount;
        set => AverageComponentCount = value;
    }

    public double IsolatedCount
    {
        get => AverageIsolatedCount;
        set => AverageIsolatedCount = value;
    }
}
