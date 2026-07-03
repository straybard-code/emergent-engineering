using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class EmergenceFingerprintService
{
    public static EmergenceFingerprint Build(
        SimulationProject project,
        string simulationName,
        int simulationProjectId,
        IReadOnlyCollection<AgentAction> actions,
        IReadOnlyCollection<KnowledgeTimelinePoint> knowledgeTimeline,
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
        var averageKnowledgeStock = knowledgeTimeline.Count == 0 ? project.KnowledgeStock : Math.Round(knowledgeTimeline.Average(item => item.KnowledgeStock), 4);
        var averageKnowledgeDiversity = knowledgeTimeline.Count == 0 ? project.KnowledgeDiversity : Math.Round(knowledgeTimeline.Average(item => item.KnowledgeDiversity), 4);
        var averageKnowledgeRewiringScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.KnowledgeRewiringScore), 4);
        var averageKnowledgeReconfigurationScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.KnowledgeReconfigurationScore), 4);
        var averageExplorationScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ExplorationScore), 4);
        var averageSerendipityScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.SerendipityScore), 4);
        var averageKnowledgeRecombinationScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.KnowledgeRecombinationScore), 4);
        var serendipityOccurredCount = knowledgeTimeline.Count(item => item.SerendipityOccurred);
        var serendipityRate = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(serendipityOccurredCount / (double)knowledgeTimeline.Count, 4);
        var serendipityToEmergenceLinkCount = knowledgeTimeline.Count(item => item.SerendipityToEmergenceLink);
        var serendipityToEmergenceRate = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(serendipityToEmergenceLinkCount / (double)knowledgeTimeline.Count, 4);
        var maxKnowledgeRewiringScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Max(item => item.KnowledgeRewiringScore), 4);
        var finalKnowledgeRewiringScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.OrderBy(item => item.StepNo).Last().KnowledgeRewiringScore, 4);
        var challengeOccurred = knowledgeTimeline.Any(item => item.ChallengeOccurred);
        var challengeResolvedPoint = knowledgeTimeline
            .Where(item => item.ChallengeResolved)
            .OrderBy(item => item.StepNo)
            .FirstOrDefault();
        var averageChallengeGap = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ChallengeGap), 4);
        var averageChallengeResolutionScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ChallengeResolutionScore), 4);
        var adaptationDuration = knowledgeTimeline.Count(item => string.Equals(item.Phase, SimulationPhase.Adaptation, StringComparison.OrdinalIgnoreCase));
        var shockPoint = knowledgeTimeline
            .Where(item => item.ShockOccurred)
            .OrderBy(item => item.StepNo)
            .FirstOrDefault();
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
            ThresholdFragilityScore = thresholdFragilityScore,
            AverageKnowledgeStock = averageKnowledgeStock,
            AverageKnowledgeDiversity = averageKnowledgeDiversity,
            AverageKnowledgeRewiringScore = averageKnowledgeRewiringScore,
            AverageExplorationScore = averageExplorationScore,
            AverageSerendipityScore = averageSerendipityScore,
            AverageKnowledgeRecombinationScore = averageKnowledgeRecombinationScore,
            SerendipityOccurredCount = serendipityOccurredCount,
            SerendipityRate = serendipityRate,
            SerendipityToEmergenceLinkCount = serendipityToEmergenceLinkCount,
            SerendipityToEmergenceRate = serendipityToEmergenceRate,
            AverageKnowledgeReconfigurationScore = averageKnowledgeReconfigurationScore,
            MaxKnowledgeRewiringScore = maxKnowledgeRewiringScore,
            FinalKnowledgeRewiringScore = finalKnowledgeRewiringScore,
            ShockOccurred = shockPoint is not null,
            ShockType = shockPoint?.ShockType ?? ShockTypes.None,
            CrossDomainExposure = project.CrossDomainExposure,
            ChallengeOccurred = challengeOccurred,
            ChallengeResolved = challengeResolvedPoint is not null,
            ChallengeResolvedStep = challengeResolvedPoint?.StepNo,
            AdaptationDuration = adaptationDuration,
            AverageChallengeGap = averageChallengeGap,
            AverageChallengeResolutionScore = averageChallengeResolutionScore
        };

        fingerprint.ClassificationLabel = Classify(fingerprint);
        fingerprint.KnowledgeDrivenType = ClassifyKnowledgeDrivenType(fingerprint);
        return fingerprint;
    }

    public static string Classify(EmergenceFingerprint fingerprint)
    {
        if (fingerprint.SerendipityToEmergenceRate >= 0.20
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "セレンディピティ駆動創発型";
        }

        if (fingerprint.SerendipityRate >= 0.20
            && (string.Equals(fingerprint.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fingerprint.FinalPhase, SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase))
            && !string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "セレンディピティ豊富な学習型";
        }

        if (fingerprint.AverageSerendipityScore < 0.25
            && fingerprint.PhaseStability >= 0.70)
        {
            return "低セレンディピティ安定型";
        }

        if (fingerprint.ChallengeResolved
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase)
            && fingerprint.AverageKnowledgeReconfigurationScore >= 0.45)
        {
            return "\u9069\u5FDC\u5275\u767A\u578B";
        }

        if (fingerprint.ChallengeResolved
            && (string.Equals(fingerprint.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fingerprint.FinalPhase, SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase))
            && !string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "\u9069\u5FDC\u5B66\u7FD2\u578B";
        }

        if (!fingerprint.ChallengeResolved
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Silo, StringComparison.OrdinalIgnoreCase))
        {
            return "\u8AB2\u984C\u672A\u89E3\u6C7A\u30B5\u30A4\u30ED\u578B";
        }

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

    public static string ClassifyKnowledgeDrivenType(EmergenceFingerprint fingerprint)
    {
        if (fingerprint.SerendipityToEmergenceRate >= 0.20
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "セレンディピティ駆動創発型";
        }

        if (fingerprint.ShockOccurred
            && fingerprint.AverageKnowledgeReconfigurationScore >= 0.50)
        {
            return "\u5916\u4E71\u99C6\u52D5\u5275\u767A\u578B";
        }

        if (fingerprint.ShockOccurred
            && fingerprint.MaxKnowledgeRewiringScore >= 0.50)
        {
            return "\u5916\u4E71\u99C6\u52D5\u5275\u767A\u578B";
        }

        if (fingerprint.AverageKnowledgeReconfigurationScore >= 0.50
            && fingerprint.CrossDomainExposure >= 0.30)
        {
            return "\u7570\u5206\u91CE\u5275\u767A\u578B";
        }

        if (fingerprint.MaxKnowledgeRewiringScore >= 0.50
            && fingerprint.CrossDomainExposure >= 0.30)
        {
            return "\u7570\u5206\u91CE\u5275\u767A\u578B";
        }

        if (fingerprint.AverageKnowledgeStock >= 0.40
            && fingerprint.FinalKnowledgeRewiringScore < 0.35
            && fingerprint.AverageKnowledgeDiversity < 0.35)
        {
            return "\u9589\u9396\u5B66\u7FD2\u578B";
        }

        if (fingerprint.AverageKnowledgeStock >= 0.40
            && fingerprint.FinalKnowledgeRewiringScore < 0.45)
        {
            return "\u77E5\u8B58\u84C4\u7A4D\u578B";
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
    public double AverageKnowledgeStock { get; init; }
    public double AverageKnowledgeDiversity { get; init; }
    public double AverageKnowledgeRewiringScore { get; init; }
    public double AverageExplorationScore { get; init; }
    public double AverageSerendipityScore { get; init; }
    public double AverageKnowledgeRecombinationScore { get; init; }
    public int SerendipityOccurredCount { get; init; }
    public double SerendipityRate { get; init; }
    public int SerendipityToEmergenceLinkCount { get; init; }
    public double SerendipityToEmergenceRate { get; init; }
    public double AverageKnowledgeReconfigurationScore { get; init; }
    public double MaxKnowledgeRewiringScore { get; init; }
    public double FinalKnowledgeRewiringScore { get; init; }
    public bool ShockOccurred { get; init; }
    public string ShockType { get; init; } = ShockTypes.None;
    public double CrossDomainExposure { get; init; }
    public bool ChallengeOccurred { get; init; }
    public bool ChallengeResolved { get; init; }
    public int? ChallengeResolvedStep { get; init; }
    public int AdaptationDuration { get; init; }
    public double AverageChallengeGap { get; init; }
    public double AverageChallengeResolutionScore { get; init; }
    public string ClassificationLabel { get; set; } = "";
    public string KnowledgeDrivenType { get; set; } = "";
}
