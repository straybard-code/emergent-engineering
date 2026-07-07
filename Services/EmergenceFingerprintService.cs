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
        var averageTrustGrowthRateEffective = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.AverageTrustGrowthRateEffective), 4);
        var averageTrustDecayApplied = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.AverageTrustDecayApplied), 4);
        var averageTrustCapacityPenalty = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.TrustCapacityPenaltyTotal), 4);
        var trustCapacityExceededRate = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Count(item => item.TrustCapacityPenaltyAppliedCount > 0) / (double)knowledgeTimeline.Count, 4);
        var strongTrustConcentration = CalculateStrongTrustConcentration(project);
        var averageRespect = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.AverageRespect), 4);
        var respectDensity = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.RespectDensity), 4);
        var respectStrongLinks = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.RespectStrongLinks), 4);
        var respectWeakLinks = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.RespectWeakLinks), 4);
        var respectConcentration = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.RespectConcentration), 4);
        var respectDiversityIndex = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.RespectDiversityIndex), 4);
        var averageChallengeAcceptanceScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ChallengeAcceptanceScore), 4);
        var averageIdeaAcceptanceScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.IdeaAcceptanceScore), 4);
        var averageRespectReconfigurationBoost = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.RespectReconfigurationBoost), 4);
        var averageRespectEmergenceComponent = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.RespectEmergenceComponent), 4);
        var averageIntellectualRespect = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.AverageIntellectualRespect), 4);
        var intellectualRespectDensity = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.IntellectualRespectDensity), 4);
        var intellectualRespectStrongLinks = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.IntellectualRespectStrongLinks), 4);
        var intellectualRespectWeakLinks = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.IntellectualRespectWeakLinks), 4);
        var intellectualRespectConcentration = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.IntellectualRespectConcentration), 4);
        var intellectualRespectDiversityIndex = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.IntellectualRespectDiversityIndex), 4);
        var mutualMentorshipScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.MutualMentorshipScore), 4);
        var averageMentorshipLinkCount = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.MentorshipLinkCount), 4);
        var averageCrossMentorshipLinkCount = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.CrossMentorshipLinkCount), 4);
        var averageLearnedFromUnexpectedAgentCount = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.LearnedFromUnexpectedAgentCount), 4);
        var averageLearningFromOthersScore = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.LearningFromOthersScore), 4);
        var averageIntellectualRespectReconfigurationComponent = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.IntellectualRespectReconfigurationComponent), 4);
        var averageIntellectualRespectSerendipityComponent = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.IntellectualRespectSerendipityComponent), 4);
        var averageIntellectualRespectEmergenceComponent = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.IntellectualRespectEmergenceComponent), 4);
        var averageEgoPenaltyApplied = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.EgoPenaltyApplied), 4);
        var averageHierarchyPenaltyApplied = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.HierarchyPenaltyApplied), 4);
        var averageThanksCoinToReconfigurationContribution = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ThanksCoinToReconfigurationContribution), 4);
        var averageThanksCoinToSerendipityContribution = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ThanksCoinToSerendipityContribution), 4);
        var averageThanksCoinToEmergenceContribution = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ThanksCoinToEmergenceContribution), 4);
        var averageThanksIdeaAsIntellectualRespectSignal = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ThanksIdeaAsIntellectualRespectSignal), 4);
        var averageThanksChallengeAsIntellectualRespectSignal = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ThanksChallengeAsIntellectualRespectSignal), 4);
        var averageThanksBridgeAsMentorshipSignal = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Average(item => item.ThanksBridgeAsMentorshipSignal), 4);
        var popularityTrapRate = knowledgeTimeline.Count == 0 ? 0 : Math.Round(knowledgeTimeline.Count(item => item.PopularityTrapDetected) / (double)knowledgeTimeline.Count, 4);
        var totalThanksCount = knowledgeTimeline.Sum(item => item.ThanksCoinCount);
        var thanksCoinRate = knowledgeTimeline.Count == 0 ? 0 : Math.Round(totalThanksCount / (double)knowledgeTimeline.Count, 4);
        var thanksHelpRate = totalThanksCount == 0 ? 0 : Math.Round(knowledgeTimeline.Sum(item => item.ThanksCoinHelpCount) / (double)totalThanksCount, 4);
        var thanksIdeaRate = totalThanksCount == 0 ? 0 : Math.Round(knowledgeTimeline.Sum(item => item.ThanksCoinIdeaCount) / (double)totalThanksCount, 4);
        var thanksChallengeRate = totalThanksCount == 0 ? 0 : Math.Round(knowledgeTimeline.Sum(item => item.ThanksCoinChallengeCount) / (double)totalThanksCount, 4);
        var thanksBridgeRate = totalThanksCount == 0 ? 0 : Math.Round(knowledgeTimeline.Sum(item => item.ThanksCoinBridgeCount) / (double)totalThanksCount, 4);
        var thanksDiversityIndex = Math.Round(1 - new[] { thanksHelpRate, thanksIdeaRate, thanksChallengeRate, thanksBridgeRate }.Sum(value => value * value), 4);
        var thanksConcentration = Math.Round(new[] { thanksHelpRate, thanksIdeaRate, thanksChallengeRate, thanksBridgeRate }.DefaultIfEmpty(0).Max(), 4);
        var thanksToEmergenceContribution = Math.Round((averageRespect * 0.30) + (thanksChallengeRate * 0.35) + (thanksBridgeRate * 0.35), 4);
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
            AverageTrustGrowthRateEffective = averageTrustGrowthRateEffective,
            AverageTrustDecayApplied = averageTrustDecayApplied,
            AverageTrustCapacityPenalty = averageTrustCapacityPenalty,
            TrustCapacityExceededRate = trustCapacityExceededRate,
            StrongTrustConcentration = strongTrustConcentration,
            AverageRespect = averageRespect,
            RespectDensity = respectDensity,
            RespectStrongLinks = respectStrongLinks,
            RespectWeakLinks = respectWeakLinks,
            RespectConcentration = respectConcentration,
            RespectDiversityIndex = respectDiversityIndex,
            AverageChallengeAcceptanceScore = averageChallengeAcceptanceScore,
            AverageIdeaAcceptanceScore = averageIdeaAcceptanceScore,
            AverageRespectReconfigurationBoost = averageRespectReconfigurationBoost,
            AverageRespectEmergenceComponent = averageRespectEmergenceComponent,
            AverageIntellectualRespect = averageIntellectualRespect,
            IntellectualRespectDensity = intellectualRespectDensity,
            IntellectualRespectStrongLinks = intellectualRespectStrongLinks,
            IntellectualRespectWeakLinks = intellectualRespectWeakLinks,
            IntellectualRespectConcentration = intellectualRespectConcentration,
            IntellectualRespectDiversityIndex = intellectualRespectDiversityIndex,
            MutualMentorshipScore = mutualMentorshipScore,
            AverageMentorshipLinkCount = averageMentorshipLinkCount,
            AverageCrossMentorshipLinkCount = averageCrossMentorshipLinkCount,
            AverageLearnedFromUnexpectedAgentCount = averageLearnedFromUnexpectedAgentCount,
            AverageLearningFromOthersScore = averageLearningFromOthersScore,
            AverageIntellectualRespectReconfigurationComponent = averageIntellectualRespectReconfigurationComponent,
            AverageIntellectualRespectSerendipityComponent = averageIntellectualRespectSerendipityComponent,
            AverageIntellectualRespectEmergenceComponent = averageIntellectualRespectEmergenceComponent,
            AverageEgoPenaltyApplied = averageEgoPenaltyApplied,
            AverageHierarchyPenaltyApplied = averageHierarchyPenaltyApplied,
            ThanksCoinRate = thanksCoinRate,
            ThanksHelpRate = thanksHelpRate,
            ThanksIdeaRate = thanksIdeaRate,
            ThanksChallengeRate = thanksChallengeRate,
            ThanksBridgeRate = thanksBridgeRate,
            ThanksDiversityIndex = thanksDiversityIndex,
            ThanksConcentration = thanksConcentration,
            ThanksToEmergenceContribution = thanksToEmergenceContribution,
            AverageThanksCoinToReconfigurationContribution = averageThanksCoinToReconfigurationContribution,
            AverageThanksCoinToSerendipityContribution = averageThanksCoinToSerendipityContribution,
            AverageThanksCoinToEmergenceContribution = averageThanksCoinToEmergenceContribution,
            AverageThanksIdeaAsIntellectualRespectSignal = averageThanksIdeaAsIntellectualRespectSignal,
            AverageThanksChallengeAsIntellectualRespectSignal = averageThanksChallengeAsIntellectualRespectSignal,
            AverageThanksBridgeAsMentorshipSignal = averageThanksBridgeAsMentorshipSignal,
            PopularityTrapRate = popularityTrapRate,
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
        fingerprint.TrustNetworkType = ClassifyTrustNetworkType(fingerprint);
        return fingerprint;
    }

    public static string Classify(EmergenceFingerprint fingerprint)
    {
        if (fingerprint.AverageIntellectualRespect >= 0.40
            && fingerprint.MutualMentorshipScore >= 0.40
            && fingerprint.AverageIntellectualRespectReconfigurationComponent >= 0.20
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "相互師匠型創発";
        }

        if (fingerprint.AverageRespect < 0.40
            && fingerprint.AverageIntellectualRespect >= 0.50
            && fingerprint.AverageChallengeAcceptanceScore >= 0.55
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "プロフェッショナル創発型";
        }

        if (fingerprint.AverageRespect >= 0.50
            && fingerprint.AverageIntellectualRespect < 0.30
            && (string.Equals(fingerprint.FinalPhase, SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fingerprint.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase)))
        {
            return "仲良し停滞型";
        }

        if (fingerprint.AverageEgoPenaltyApplied >= 0.20
            && fingerprint.AverageIdeaAcceptanceScore < 0.40
            && fingerprint.AverageKnowledgeReconfigurationScore < 0.40)
        {
            return "自尊心障壁型";
        }

        if (fingerprint.AverageHierarchyPenaltyApplied >= 0.20
            && fingerprint.AverageLearnedFromUnexpectedAgentCount < 1
            && fingerprint.AverageCrossMentorshipLinkCount < 1)
        {
            return "序列障壁型";
        }

        if (fingerprint.AverageRespect >= 0.50
            && fingerprint.ThanksChallengeRate > 0.10
            && fingerprint.ThanksBridgeRate > 0.10
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "敬意駆動創発型";
        }

        if (fingerprint.AverageRespect >= 0.40
            && fingerprint.AverageChallengeAcceptanceScore >= 0.55
            && fingerprint.ThanksChallengeRate >= 0.05
            && fingerprint.ThanksBridgeRate >= 0.05
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "敬意駆動創発型";
        }

        if (fingerprint.ThanksConcentration > 0.60
            && fingerprint.AverageRespect >= 0.50
            && fingerprint.SerendipityRate == 0
            && (string.Equals(fingerprint.FinalPhase, SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fingerprint.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase)))
        {
            return "人気投票停滞型";
        }

        if (fingerprint.ThanksConcentration >= 0.60
            && fingerprint.PopularityTrapRate >= 0.40
            && !string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(fingerprint.FinalPhase, SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fingerprint.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase)))
        {
            return "人気投票停滞型";
        }

        if (fingerprint.AverageRespect < 0.30
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase)
            && fingerprint.AverageKnowledgeReconfigurationScore < 0.40)
        {
            return "低敬意学習型";
        }

        if (fingerprint.AverageRespect >= 0.40
            && fingerprint.AverageChallengeAcceptanceScore >= 0.55
            && string.Equals(fingerprint.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fingerprint.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "敬意ある学習型";
        }
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

    public static string ClassifyTrustNetworkType(EmergenceFingerprint fingerprint)
    {
        if (fingerprint.AverageTrust > 0.95
            && fingerprint.EffectiveDensity > 0.95
            && fingerprint.ThresholdFragilityScore < 0.05)
        {
            return "過信完全ネットワーク型";
        }

        if (fingerprint.AverageTrust >= 0.5
            && fingerprint.AverageTrust <= 0.9
            && fingerprint.EffectiveDensity >= 0.4
            && fingerprint.EffectiveDensity <= 0.9
            && fingerprint.StrongTrustConcentration >= 0.6)
        {
            return "選択的信頼ネットワーク型";
        }

        if (fingerprint.AverageTrust < 0.4
            && (fingerprint.ComponentCount > 1 || fingerprint.IsolatedCount > 0))
        {
            return "脆弱信頼ネットワーク型";
        }

        return "未分類";
    }

    private static double CalculateStrongTrustConcentration(SimulationProject project)
    {
        if (project.Agents.Count == 0 || project.TrustCapacity <= 0)
        {
            return 0;
        }

        var trustMaps = project.Agents.ToDictionary(
            agent => agent.Id,
            agent => TrustJsonUtility.Deserialize(agent.TrustJson));

        var values = project.Agents
            .Select(sourceAgent =>
            {
                var outboundPositiveTrusts = project.Agents
                    .Where(targetAgent => targetAgent.Id != sourceAgent.Id)
                    .Select(targetAgent =>
                    {
                        var trustMap = trustMaps[sourceAgent.Id];
                        var trustValue = trustMap.TryGetValue(targetAgent.Name, out var value) ? value : 0;
                        return Math.Max(0, trustValue);
                    })
                    .Where(value => value > 0)
                    .OrderByDescending(value => value)
                    .ToList();

                if (outboundPositiveTrusts.Count == 0)
                {
                    return 0.0;
                }

                var totalPositive = outboundPositiveTrusts.Sum();
                if (totalPositive <= 0)
                {
                    return 0.0;
                }

                var topCount = Math.Max(1, Math.Min(project.TrustCapacity, outboundPositiveTrusts.Count));
                var topSum = outboundPositiveTrusts.Take(topCount).Sum();
                return Math.Clamp(topSum / totalPositive, 0, 1);
            })
            .ToList();

        return values.Count == 0 ? 0 : Math.Round(values.Average(), 4);
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
    public double AverageTrustGrowthRateEffective { get; init; }
    public double AverageTrustDecayApplied { get; init; }
    public double AverageTrustCapacityPenalty { get; init; }
    public double TrustCapacityExceededRate { get; init; }
    public double StrongTrustConcentration { get; init; }
    public double AverageRespect { get; init; }
    public double RespectDensity { get; init; }
    public double RespectStrongLinks { get; init; }
    public double RespectWeakLinks { get; init; }
    public double RespectConcentration { get; init; }
    public double RespectDiversityIndex { get; init; }
    public double AverageChallengeAcceptanceScore { get; init; }
    public double AverageIdeaAcceptanceScore { get; init; }
    public double AverageRespectReconfigurationBoost { get; init; }
    public double AverageRespectEmergenceComponent { get; init; }
    public double AverageIntellectualRespect { get; init; }
    public double IntellectualRespectDensity { get; init; }
    public double IntellectualRespectStrongLinks { get; init; }
    public double IntellectualRespectWeakLinks { get; init; }
    public double IntellectualRespectConcentration { get; init; }
    public double IntellectualRespectDiversityIndex { get; init; }
    public double MutualMentorshipScore { get; init; }
    public double AverageMentorshipLinkCount { get; init; }
    public double AverageCrossMentorshipLinkCount { get; init; }
    public double AverageLearnedFromUnexpectedAgentCount { get; init; }
    public double AverageLearningFromOthersScore { get; init; }
    public double AverageIntellectualRespectReconfigurationComponent { get; init; }
    public double AverageIntellectualRespectSerendipityComponent { get; init; }
    public double AverageIntellectualRespectEmergenceComponent { get; init; }
    public double AverageEgoPenaltyApplied { get; init; }
    public double AverageHierarchyPenaltyApplied { get; init; }
    public double ThanksCoinRate { get; init; }
    public double ThanksHelpRate { get; init; }
    public double ThanksIdeaRate { get; init; }
    public double ThanksChallengeRate { get; init; }
    public double ThanksBridgeRate { get; init; }
    public double ThanksDiversityIndex { get; init; }
    public double ThanksConcentration { get; init; }
    public double ThanksToEmergenceContribution { get; init; }
    public double AverageThanksCoinToReconfigurationContribution { get; init; }
    public double AverageThanksCoinToSerendipityContribution { get; init; }
    public double AverageThanksCoinToEmergenceContribution { get; init; }
    public double AverageThanksIdeaAsIntellectualRespectSignal { get; init; }
    public double AverageThanksChallengeAsIntellectualRespectSignal { get; init; }
    public double AverageThanksBridgeAsMentorshipSignal { get; init; }
    public double PopularityTrapRate { get; init; }
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
    public string TrustNetworkType { get; set; } = "";
}
