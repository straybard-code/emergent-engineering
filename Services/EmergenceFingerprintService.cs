using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class EmergenceFingerprintService
{
    public static EmergenceFingerprint Build(
        SimulationProject project,
        string simulationName,
        int simulationProjectId,
        IReadOnlyCollection<AgentAction> actions,
        IReadOnlyCollection<PhaseTransitionInsight> phaseTransitionInsights,
        IReadOnlyCollection<ThresholdSweepPoint> thresholdSweep,
        NetworkMetricsResult finalMetrics,
        int? runNo = null)
    {
        var actionDistribution = ActionDistributionCalculator.Calculate(actions.Select(action => action.Action));
        var triggerAgentStats = phaseTransitionInsights
            .SelectMany(insight => insight.TriggerAgents)
            .GroupBy(agent => agent.AgentName)
            .Select(group => new
            {
                AgentName = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.AgentName)
            .ToList();

        var topTriggerAgent = triggerAgentStats.FirstOrDefault();
        var thresholdFragilityScore = thresholdSweep.Count == 0
            ? 0
            : Math.Round(1 - thresholdSweep.Average(point => point.EffectiveNetworkDensity), 4);

        var fingerprint = new EmergenceFingerprint
        {
            RunNo = runNo,
            SimulationProjectId = simulationProjectId,
            SimulationName = simulationName,
            FinalPhase = project.Metrics?.FinalPhase ?? project.Phase,
            AverageTrust = finalMetrics.AverageTrust,
            EffectiveDensity = finalMetrics.EffectiveNetworkDensity,
            NetworkDensity = finalMetrics.NetworkDensity,
            StrongLinkCount = finalMetrics.StrongLinkCount,
            WeakLinkCount = finalMetrics.WeakLinkCount,
            ComponentCount = finalMetrics.ComponentCount,
            IsolatedCount = finalMetrics.IsolatedCount,
            PhaseTransitionCount = project.Metrics?.PhaseChangeCount ?? phaseTransitionInsights.Count,
            PhaseStability = project.Metrics?.PhaseStability ?? 0,
            ShareInfoRate = actionDistribution.ShareInfoRate,
            AskHelpRate = actionDistribution.AskHelpRate,
            ProposeIdeaRate = actionDistribution.ProposeIdeaRate,
            CriticizeRate = actionDistribution.CriticizeRate,
            WorkAloneRate = actionDistribution.WorkAloneRate,
            SupportOtherRate = actionDistribution.SupportOtherRate,
            WaitRate = actionDistribution.WaitRate,
            TriggerAgentDiversity = triggerAgentStats.Count,
            TopTriggerAgentName = topTriggerAgent?.AgentName ?? "-",
            TopTriggerAgentCount = topTriggerAgent?.Count ?? 0,
            AverageNewStrongLinksPerTransition = phaseTransitionInsights.Count == 0
                ? 0
                : Math.Round(phaseTransitionInsights.Average(item => item.NewStrongLinkTotalCount), 4),
            AverageLostStrongLinksPerTransition = phaseTransitionInsights.Count == 0
                ? 0
                : Math.Round(phaseTransitionInsights.Average(item => item.LostStrongLinkTotalCount), 4),
            ThresholdFragilityScore = thresholdFragilityScore
        };

        fingerprint.ClassificationLabel = Classify(fingerprint);
        return fingerprint;
    }

    public static string Classify(EmergenceFingerprint fingerprint)
    {
        if (fingerprint.EffectiveDensity > 0.8
            && fingerprint.ShareInfoRate > 0.2
            && fingerprint.WorkAloneRate < 0.3)
        {
            return "\u7D71\u5408\u5B66\u7FD2\u578B";
        }

        if (string.Equals(fingerprint.FinalPhase, SimulationPhase.Silo, StringComparison.OrdinalIgnoreCase)
            && fingerprint.WorkAloneRate > 0.35
            && fingerprint.EffectiveDensity > 0.7)
        {
            return "\u7D71\u5236\u30B5\u30A4\u30ED\u578B";
        }

        if (fingerprint.ThresholdFragilityScore > 0.5
            && fingerprint.EffectiveDensity < 0.6)
        {
            return "\u8106\u5F31\u30CD\u30C3\u30C8\u30EF\u30FC\u30AF\u578B";
        }

        if (fingerprint.TopTriggerAgentCount >= 2
            && fingerprint.TriggerAgentDiversity <= 2)
        {
            return "\u89E6\u5A92\u4F9D\u5B58\u578B";
        }

        return "\u672A\u5206\u985E";
    }
}

public sealed class EmergenceFingerprint
{
    public int? RunNo { get; init; }
    public int SimulationProjectId { get; init; }
    public string SimulationName { get; init; } = "";
    public string FinalPhase { get; init; } = "";
    public double AverageTrust { get; init; }
    public double EffectiveDensity { get; init; }
    public double NetworkDensity { get; init; }
    public int StrongLinkCount { get; init; }
    public int WeakLinkCount { get; init; }
    public int ComponentCount { get; init; }
    public int IsolatedCount { get; init; }
    public int PhaseTransitionCount { get; init; }
    public double PhaseStability { get; init; }
    public double ShareInfoRate { get; init; }
    public double AskHelpRate { get; init; }
    public double ProposeIdeaRate { get; init; }
    public double CriticizeRate { get; init; }
    public double WorkAloneRate { get; init; }
    public double SupportOtherRate { get; init; }
    public double WaitRate { get; init; }
    public int TriggerAgentDiversity { get; init; }
    public string TopTriggerAgentName { get; init; } = "-";
    public int TopTriggerAgentCount { get; init; }
    public double AverageNewStrongLinksPerTransition { get; init; }
    public double AverageLostStrongLinksPerTransition { get; init; }
    public double ThresholdFragilityScore { get; init; }
    public string ClassificationLabel { get; set; } = "";
}
