using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class PhaseTransitionInspector
{
    public static List<PhaseTransitionInsight> BuildInsights(
        IReadOnlyCollection<PhaseTransitionStepState> stepStates,
        IReadOnlyCollection<PhaseTransitionActionRecord>? actionRecords = null,
        IReadOnlyDictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>>? edgeRowsByStep = null,
        int windowSize = 3,
        double effectiveTrustThreshold = 0.30)
    {
        var orderedStates = stepStates
            .OrderBy(state => state.StepNo)
            .ToList();
        var actionList = actionRecords?.OrderBy(record => record.StepNo).ToList() ?? new List<PhaseTransitionActionRecord>();

        List<PhaseTransitionInsight> insights = [];
        for (var index = 1; index < orderedStates.Count; index++)
        {
            var before = orderedStates[index - 1];
            var after = orderedStates[index];
            if (string.Equals(before.Phase, after.Phase, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var insight = new PhaseTransitionInsight
            {
                StepNo = after.StepNo,
                FromPhase = before.Phase,
                ToPhase = after.Phase,
                FromPhaseLabel = FormatPhaseLabel(before.Phase),
                ToPhaseLabel = FormatPhaseLabel(after.Phase),
                ShareInfoDelta = RoundDelta(after.ShareInfoRate - before.ShareInfoRate),
                AskHelpDelta = RoundDelta(after.AskHelpRate - before.AskHelpRate),
                ProposeIdeaDelta = RoundDelta(after.ProposeIdeaRate - before.ProposeIdeaRate),
                CriticizeDelta = RoundDelta(after.CriticizeRate - before.CriticizeRate),
                WorkAloneDelta = RoundDelta(after.WorkAloneRate - before.WorkAloneRate),
                SupportOtherDelta = RoundDelta(after.SupportOtherRate - before.SupportOtherRate),
                WaitDelta = RoundDelta(after.WaitRate - before.WaitRate),
                OtherDelta = RoundDelta(after.OtherRate - before.OtherRate),
                AverageTrustBefore = before.AverageTrust,
                AverageTrustAfter = after.AverageTrust,
                AverageTrustDelta = RoundDelta(after.AverageTrust - before.AverageTrust),
                EffectiveDensityBefore = before.EffectiveNetworkDensity,
                EffectiveDensityAfter = after.EffectiveNetworkDensity,
                EffectiveDensityDelta = RoundDelta(after.EffectiveNetworkDensity - before.EffectiveNetworkDensity),
                StrongLinksBefore = before.StrongLinkCount,
                StrongLinksAfter = after.StrongLinkCount,
                StrongLinksDelta = after.StrongLinkCount - before.StrongLinkCount,
                WeakLinksBefore = before.WeakLinkCount,
                WeakLinksAfter = after.WeakLinkCount,
                WeakLinksDelta = after.WeakLinkCount - before.WeakLinkCount,
                ComponentCountBefore = before.ComponentCount,
                ComponentCountAfter = after.ComponentCount,
                ComponentCountDelta = after.ComponentCount - before.ComponentCount,
                IsolatedCountBefore = before.IsolatedCount,
                IsolatedCountAfter = after.IsolatedCount,
                IsolatedCountDelta = after.IsolatedCount - before.IsolatedCount,
                ChallengeActive = after.ChallengeActive,
                ChallengeOccurred = after.ChallengeOccurred,
                ChallengeResolved = after.ChallengeResolved,
                ChallengeResolutionScore = after.ChallengeResolutionScore,
                ChallengeGap = after.ChallengeGap,
                KnowledgeReconfigurationScore = after.KnowledgeReconfigurationScore,
                ExplorationScore = after.ExplorationScore,
                SerendipityScore = after.SerendipityScore,
                SerendipityOccurred = after.SerendipityOccurred,
                KnowledgeRecombinationScore = after.KnowledgeRecombinationScore,
                SerendipityDrivenReconfiguration = after.SerendipityDrivenReconfiguration,
                SerendipityToEmergenceLink = after.SerendipityToEmergenceLink
            };

            insight.MainActionChange = DescribeMainActionChange(insight);
            insight.MainIncreasingAction = DescribeDirectionalActionChange(insight, positive: true);
            insight.MainDecreasingAction = DescribeDirectionalActionChange(insight, positive: false);
            insight.MainNetworkChange = DescribeMainNetworkChange(insight);
            EnrichWithTriggerWindow(insight, actionList, edgeRowsByStep, windowSize, effectiveTrustThreshold);
            insight.ShortInterpretation = BuildShortInterpretation(insight);
            insight.ExplainerText = BuildExplainerText(insight, windowSize);

            insights.Add(insight);
        }

        return insights;
    }

    public static string BuildMostCommonPatternLabel(IReadOnlyCollection<PhaseTransitionInsight> insights)
    {
        if (insights.Count == 0)
        {
            return "-";
        }

        var group = insights
            .GroupBy(insight => new { insight.FromPhaseLabel, insight.ToPhaseLabel })
            .OrderByDescending(item => item.Count())
            .ThenBy(item => item.Key.FromPhaseLabel)
            .ThenBy(item => item.Key.ToPhaseLabel)
            .First();

        return $"{group.Key.FromPhaseLabel} -> {group.Key.ToPhaseLabel}: {group.Count()}\u56de";
    }

    public static List<PhaseTransitionPatternSummary> BuildPatternSummaries(IReadOnlyCollection<PhaseTransitionInsight> insights)
    {
        return insights
            .GroupBy(insight => new { insight.FromPhase, insight.ToPhase })
            .Select(group => new PhaseTransitionPatternSummary
            {
                FromPhase = group.Key.FromPhase,
                ToPhase = group.Key.ToPhase,
                Count = group.Count(),
                AverageShareInfoDelta = Math.Round(group.Average(item => item.ShareInfoDelta), 4),
                AverageProposeIdeaDelta = Math.Round(group.Average(item => item.ProposeIdeaDelta), 4),
                AverageWorkAloneDelta = Math.Round(group.Average(item => item.WorkAloneDelta), 4),
                AverageSupportOtherDelta = Math.Round(group.Average(item => item.SupportOtherDelta), 4),
                AverageEffectiveDensityDelta = Math.Round(group.Average(item => item.EffectiveDensityDelta), 4),
                AverageAverageTrustDelta = Math.Round(group.Average(item => item.AverageTrustDelta), 4),
                AverageNewStrongLinkCount = Math.Round(group.Average(item => item.NewStrongLinkTotalCount), 4),
                AverageLostStrongLinkCount = Math.Round(group.Average(item => item.LostStrongLinkTotalCount), 4)
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => GetPhaseOrder(item.FromPhase))
            .ThenBy(item => GetPhaseOrder(item.ToPhase))
            .ToList();
    }

    public static List<TriggerAgentRankingSummary> BuildTriggerAgentRankings(IReadOnlyCollection<PhaseTransitionInsight> insights)
    {
        return insights
            .SelectMany(insight => insight.TriggerAgents)
            .GroupBy(agent => agent.AgentName)
            .Select(group => new TriggerAgentRankingSummary
            {
                AgentName = group.Key,
                Count = group.Count(),
                AverageTriggerScore = Math.Round(group.Average(item => item.TriggerScore), 2)
            })
            .OrderByDescending(item => item.Count)
            .ThenByDescending(item => item.AverageTriggerScore)
            .ThenBy(item => item.AgentName)
            .ToList();
    }

    public static string FormatPhaseLabel(string? phase) => phase switch
    {
        "Forming" => "Forming\uFF08\u5F62\u6210\u671F\uFF09",
        "Learning" => "Learning\uFF08\u5B66\u7FD2\u671F\uFF09",
        "Adaptation" => "Adaptation\uFF08\u9069\u5FDC\u671F\uFF09",
        "Stable" => "Stable\uFF08\u5B89\u5B9A\u671F\uFF09",
        "Emergent" => "Emergent\uFF08\u5275\u767A\u671F\uFF09",
        "Silo" => "Silo\uFF08\u30B5\u30A4\u30ED\u5316\uFF09",
        "Chaos" => "Chaos\uFF08\u6DF7\u4E71\uFF09",
        "Collapse" => "Collapse\uFF08\u5D29\u58CA\uFF09",
        _ => phase ?? "-"
    };

    private static string DescribeMainActionChange(PhaseTransitionInsight insight)
    {
        var candidates = new (string Label, double Delta)[]
        {
            (ActionDistributionCalculator.GetDisplayLabel("ShareInfo"), insight.ShareInfoDelta),
            (ActionDistributionCalculator.GetDisplayLabel("AskHelp"), insight.AskHelpDelta),
            (ActionDistributionCalculator.GetDisplayLabel("ProposeIdea"), insight.ProposeIdeaDelta),
            (ActionDistributionCalculator.GetDisplayLabel("Criticize"), insight.CriticizeDelta),
            (ActionDistributionCalculator.GetDisplayLabel("WorkAlone"), insight.WorkAloneDelta),
            (ActionDistributionCalculator.GetDisplayLabel("SupportOther"), insight.SupportOtherDelta),
            (ActionDistributionCalculator.GetDisplayLabel("Wait"), insight.WaitDelta),
            ("Other", insight.OtherDelta)
        };

        var winner = candidates
            .OrderByDescending(item => Math.Abs(item.Delta))
            .First();

        if (Math.Abs(winner.Delta) < 0.01)
        {
            return "\u5927\u304D\u306A\u884C\u52D5\u5909\u5316\u306A\u3057";
        }

        var direction = winner.Delta >= 0 ? "\u5897\u52A0" : "\u6E1B\u5C11";
        return $"{winner.Label}が {Math.Abs(winner.Delta).ToString("0.00")} {direction}";
    }

    private static string DescribeDirectionalActionChange(PhaseTransitionInsight insight, bool positive)
    {
        var candidates = new (string ActionName, double Delta)[]
        {
            ("ShareInfo", insight.ShareInfoDelta),
            ("AskHelp", insight.AskHelpDelta),
            ("ProposeIdea", insight.ProposeIdeaDelta),
            ("Criticize", insight.CriticizeDelta),
            ("WorkAlone", insight.WorkAloneDelta),
            ("SupportOther", insight.SupportOtherDelta),
            ("Wait", insight.WaitDelta),
            (ActionDistributionCalculator.OtherAction, insight.OtherDelta)
        };

        var filtered = positive
            ? candidates.Where(item => item.Delta > 0).OrderByDescending(item => item.Delta).ToList()
            : candidates.Where(item => item.Delta < 0).OrderBy(item => item.Delta).ToList();

        if (filtered.Count == 0)
        {
            return "\u306A\u3057";
        }

        var winner = filtered[0];
        var label = ActionDistributionCalculator.GetDisplayLabel(winner.ActionName);
        return $"{label} {Math.Abs(winner.Delta).ToString("0.00")}";
    }

    private static void EnrichWithTriggerWindow(
        PhaseTransitionInsight insight,
        IReadOnlyCollection<PhaseTransitionActionRecord> actionRecords,
        IReadOnlyDictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>>? edgeRowsByStep,
        int windowSize,
        double effectiveTrustThreshold)
    {
        var windowStart = Math.Max(1, insight.StepNo - windowSize);
        var windowActions = actionRecords
            .Where(record => record.StepNo >= windowStart && record.StepNo <= insight.StepNo)
            .ToList();

        insight.WindowDominantAction = DescribeWindowDominantAction(windowActions);
        insight.TriggerAgents = BuildTriggerAgents(windowActions);
        insight.TriggerTrustChanges = BuildTriggerTrustChanges(windowActions);
        var newStrongLinks = BuildEdgeChanges(edgeRowsByStep, insight.StepNo, effectiveTrustThreshold, becameStrong: true);
        var lostStrongLinks = BuildEdgeChanges(edgeRowsByStep, insight.StepNo, effectiveTrustThreshold, becameStrong: false);
        insight.NewStrongLinkTotalCount = newStrongLinks.Count;
        insight.LostStrongLinkTotalCount = lostStrongLinks.Count;
        insight.NewStrongLinks = newStrongLinks.Take(5).ToList();
        insight.LostStrongLinks = lostStrongLinks.Take(5).ToList();
    }

    private static string DescribeWindowDominantAction(IReadOnlyCollection<PhaseTransitionActionRecord> actions)
    {
        if (actions.Count == 0)
        {
            return "\u306A\u3057";
        }

        var winner = actions
            .GroupBy(action => ActionDistributionCalculator.Normalize(action.Action))
            .Select(group => new
            {
                Action = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Action)
            .First();

        return ActionDistributionCalculator.GetDisplayLabel(winner.Action);
    }

    private static List<TriggerAgentInsight> BuildTriggerAgents(IReadOnlyCollection<PhaseTransitionActionRecord> actions)
    {
        return actions
            .GroupBy(action => action.AgentName)
            .Select(group =>
            {
                var shareInfoCount = group.Count(item => ActionDistributionCalculator.Normalize(item.Action) == "ShareInfo");
                var proposeIdeaCount = group.Count(item => ActionDistributionCalculator.Normalize(item.Action) == "ProposeIdea");
                var supportOtherCount = group.Count(item => ActionDistributionCalculator.Normalize(item.Action) == "SupportOther");
                var criticizeCount = group.Count(item => ActionDistributionCalculator.Normalize(item.Action) == "Criticize");
                var askHelpCount = group.Count(item => ActionDistributionCalculator.Normalize(item.Action) == "AskHelp");
                var workAloneCount = group.Count(item => ActionDistributionCalculator.Normalize(item.Action) == "WorkAlone");
                var waitCount = group.Count(item => ActionDistributionCalculator.Normalize(item.Action) == "Wait");
                var trustDeltaSum = Math.Round(group.Sum(item => item.TrustDelta ?? 0), 4);

                var dominantAction = group
                    .GroupBy(item => ActionDistributionCalculator.Normalize(item.Action))
                    .Select(item => new { Action = item.Key, Count = item.Count() })
                    .OrderByDescending(item => item.Count)
                    .ThenBy(item => item.Action)
                    .FirstOrDefault();

                var triggerScore = (shareInfoCount * 2)
                    + (proposeIdeaCount * 2)
                    + (supportOtherCount * 2)
                    + criticizeCount
                    + (Math.Abs(trustDeltaSum) * 5);

                return new TriggerAgentInsight
                {
                    AgentName = group.Key,
                    ActionCount = group.Count(),
                    ShareInfoCount = shareInfoCount,
                    AskHelpCount = askHelpCount,
                    ProposeIdeaCount = proposeIdeaCount,
                    CriticizeCount = criticizeCount,
                    WorkAloneCount = workAloneCount,
                    SupportOtherCount = supportOtherCount,
                    WaitCount = waitCount,
                    TrustDeltaSum = trustDeltaSum,
                    TriggerScore = Math.Round(triggerScore, 2),
                    MainAction = dominantAction is null
                        ? "\u306A\u3057"
                        : ActionDistributionCalculator.GetDisplayLabel(dominantAction.Action)
                };
            })
            .OrderByDescending(agent => agent.TriggerScore)
            .ThenByDescending(agent => agent.ActionCount)
            .ThenBy(agent => agent.AgentName)
            .Take(3)
            .ToList();
    }

    private static List<TriggerTrustChangeInsight> BuildTriggerTrustChanges(IReadOnlyCollection<PhaseTransitionActionRecord> actions)
    {
        return actions
            .Where(action => action.TrustDelta.HasValue)
            .OrderByDescending(action => Math.Abs(action.TrustDelta ?? 0))
            .ThenByDescending(action => action.StepNo)
            .Take(5)
            .Select(action => new TriggerTrustChangeInsight
            {
                StepNo = action.StepNo,
                AgentName = action.AgentName,
                Action = action.Action,
                TargetAgentName = string.IsNullOrWhiteSpace(action.TargetAgentName) ? "-" : action.TargetAgentName,
                TrustBefore = action.TrustBefore,
                TrustDelta = action.TrustDelta ?? 0,
                TrustAfter = action.TrustAfter
            })
            .ToList();
    }

    private static List<TriggerEdgeChange> BuildEdgeChanges(
        IReadOnlyDictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>>? edgeRowsByStep,
        int transitionStep,
        double effectiveTrustThreshold,
        bool becameStrong)
    {
        if (edgeRowsByStep is null || transitionStep <= 1)
        {
            return new List<TriggerEdgeChange>();
        }

        if (!edgeRowsByStep.TryGetValue(transitionStep - 1, out var beforeRows)
            || !edgeRowsByStep.TryGetValue(transitionStep, out var afterRows))
        {
            return new List<TriggerEdgeChange>();
        }

        var beforeMap = beforeRows.ToDictionary(
            row => (row.SourceAgentName, row.TargetAgentName),
            row => row.TrustValue);
        var afterMap = afterRows.ToDictionary(
            row => (row.SourceAgentName, row.TargetAgentName),
            row => row.TrustValue);

        return afterMap
            .Select(pair =>
            {
                beforeMap.TryGetValue(pair.Key, out var beforeValue);
                var afterValue = pair.Value;
                return new TriggerEdgeChange
                {
                    SourceAgentName = pair.Key.SourceAgentName,
                    TargetAgentName = pair.Key.TargetAgentName,
                    BeforeValue = Math.Round(beforeValue, 2),
                    AfterValue = Math.Round(afterValue, 2)
                };
            })
            .Where(item => becameStrong
                ? beforeMap.GetValueOrDefault((item.SourceAgentName, item.TargetAgentName)) < effectiveTrustThreshold
                    && item.AfterValue >= effectiveTrustThreshold
                : beforeMap.GetValueOrDefault((item.SourceAgentName, item.TargetAgentName)) >= effectiveTrustThreshold
                    && item.AfterValue < effectiveTrustThreshold)
            .OrderByDescending(item => Math.Abs(item.AfterValue - item.BeforeValue))
            .ThenBy(item => item.SourceAgentName)
            .ThenBy(item => item.TargetAgentName)
            .ToList();
    }

    private static string DescribeMainNetworkChange(PhaseTransitionInsight insight)
    {
        if (insight.ComponentCountDelta > 0)
        {
            return "\u9023\u7D50\u6210\u5206\u6570\u304C\u5897\u52A0\u3057\u3001\u30CD\u30C3\u30C8\u30EF\u30FC\u30AF\u5206\u65AD\u304C\u9032\u884C";
        }

        if (insight.ComponentCountDelta < 0)
        {
            return "\u9023\u7D50\u6210\u5206\u6570\u304C\u6E1B\u5C11\u3057\u3001\u30CD\u30C3\u30C8\u30EF\u30FC\u30AF\u7D71\u5408\u304C\u9032\u884C";
        }

        if (insight.EffectiveDensityDelta > 0.05)
        {
            return "\u5B9F\u52B9\u30CD\u30C3\u30C8\u30EF\u30FC\u30AF\u5BC6\u5EA6\u304C\u4E0A\u6607";
        }

        if (insight.EffectiveDensityDelta < -0.05)
        {
            return "\u5B9F\u52B9\u30CD\u30C3\u30C8\u30EF\u30FC\u30AF\u5BC6\u5EA6\u304C\u4F4E\u4E0B";
        }

        if (insight.AverageTrustDelta > 0.05)
        {
            return "\u5E73\u5747\u4FE1\u983C\u5EA6\u304C\u4E0A\u6607";
        }

        if (insight.AverageTrustDelta < -0.05)
        {
            return "\u5E73\u5747\u4FE1\u983C\u5EA6\u304C\u4F4E\u4E0B";
        }

        return "\u30CD\u30C3\u30C8\u30EF\u30FC\u30AF\u6307\u6A19\u306B\u5927\u304D\u306A\u5909\u5316\u306A\u3057";
    }

    private static string BuildShortInterpretation(PhaseTransitionInsight insight)
    {
        return $"{insight.FromPhaseLabel}\u304B\u3089{insight.ToPhaseLabel}\u3078\u79FB\u884C\u3057\u307E\u3057\u305F\u3002{insight.MainActionChange}\u3002{insight.MainNetworkChange}\u3002";
    }

    private static string BuildExplainerText(PhaseTransitionInsight insight, int windowSize)
    {
        var topAgents = insight.TriggerAgents
            .Take(2)
            .Select(agent => agent.AgentName)
            .ToList();

        var agentText = topAgents.Count switch
        {
            0 => "\u76EE\u7ACB\u3064\u5F15\u304D\u91D1\u30A8\u30FC\u30B8\u30A7\u30F3\u30C8\u306F\u3042\u308A\u307E\u305B\u3093\u3067\u3057\u305F",
            1 => $"{topAgents[0]}\u306E\u884C\u52D5\u304C\u76EE\u7ACB\u3061",
            _ => $"{topAgents[0]}\u3068 {topAgents[1]}\u306E\u884C\u52D5\u304C\u76EE\u7ACB\u3061"
        };

        var edgeText = insight.NewStrongLinks.Count > 0
            ? $"\u65B0\u3057\u3044Strong Link\uFF08{FormatEdge(insight.NewStrongLinks[0])}\uFF09\u304C\u5F62\u6210\u3055\u308C"
            : insight.LostStrongLinks.Count > 0
                ? $"Strong Link\uFF08{FormatEdge(insight.LostStrongLinks[0])}\uFF09\u304C\u5F31\u307E\u308A"
                : "\u5F37\u3044\u30EA\u30F3\u30AF\u69CB\u9020\u306B\u5927\u304D\u306A\u51FA\u5165\u308A\u306F\u306A\u304F";

        var causeText = insight.ToPhase switch
        {
            "Adaptation" => "\u77e5\u8b58\u518d\u69cb\u6210\u3068\u8de8\u5883\u754c\u5354\u50cd\u304c\u59cb\u307e\u3063\u305f\u53ef\u80fd\u6027\u304c\u3042\u308a\u307e\u3059",
            "Emergent" => "\u5C40\u6240\u7684\u306A\u5354\u50CD\u95A2\u4FC2\u304C\u5F37\u307E\u3063\u305F\u3053\u3068\u304C\u5275\u767A\u306E\u5F15\u304D\u91D1\u306B\u306A\u3063\u305F\u53EF\u80FD\u6027\u304C\u3042\u308A\u307E\u3059",
            "Learning" => "\u5B66\u7FD2\u7684\u306A\u76F8\u4E92\u4F5C\u7528\u304C\u5F37\u307E\u3063\u305F\u53EF\u80FD\u6027\u304C\u3042\u308A\u307E\u3059",
            "Stable" => "\u884C\u52D5\u5206\u5E03\u3068\u30CD\u30C3\u30C8\u30EF\u30FC\u30AF\u304C\u5B89\u5B9A\u5074\u306B\u5BC4\u3063\u305F\u53EF\u80FD\u6027\u304C\u3042\u308A\u307E\u3059",
            "Silo" => "\u884C\u52D5\u5206\u5E03\u304C\u500B\u5225\u6700\u9069\u5074\u306B\u5BC4\u3063\u305F\u3053\u3068\u304C\u30B5\u30A4\u30ED\u5316\u306E\u8981\u56E0\u306B\u306A\u3063\u305F\u53EF\u80FD\u6027\u304C\u3042\u308A\u307E\u3059",
            "Chaos" => "\u652F\u63F4\u3088\u308A\u3082\u5BFE\u7ACB\u7684\u306A\u5909\u5316\u304C\u76EE\u7ACB\u3063\u305F\u53EF\u80FD\u6027\u304C\u3042\u308A\u307E\u3059",
            "Collapse" => "\u5354\u50CD\u306E\u4F4E\u4E0B\u3068\u505C\u6EDE\u304C\u9032\u3093\u3060\u53EF\u80FD\u6027\u304C\u3042\u308A\u307E\u3059",
            _ => "\u884C\u52D5\u3068\u30CD\u30C3\u30C8\u30EF\u30FC\u30AF\u306E\u5FAE\u5C0F\u306A\u5909\u5316\u304C\u79FB\u884C\u306B\u95A2\u4E0E\u3057\u305F\u53EF\u80FD\u6027\u304C\u3042\u308A\u307E\u3059"
        };

        var challengeText = "";
        if (insight.ChallengeResolved && string.Equals(insight.ToPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            challengeText = " Challengeが解決され、再配線された知識ネットワークをもとに創発相へ移行した可能性があります。";
        }
        else if (insight.ChallengeActive && string.Equals(insight.ToPhase, SimulationPhase.Adaptation, StringComparison.OrdinalIgnoreCase))
        {
            challengeText = " Challenge発生後、知識再構成スコアが上昇し、適応相へ移行した可能性があります。";
        }
        else if (!insight.ChallengeResolved && string.Equals(insight.ToPhase, SimulationPhase.Silo, StringComparison.OrdinalIgnoreCase))
        {
            challengeText = " ChallengeGapが残ったまま単独作業側へ寄り、サイロ化した可能性があります。";
        }
        else if (insight.ChallengeActive || insight.ChallengeOccurred || insight.ChallengeResolved)
        {
            challengeText = $" Challenge resolution={insight.ChallengeResolutionScore:0.00}, gap={insight.ChallengeGap:+0.00;-0.00;0.00}, reconfiguration={insight.KnowledgeReconfigurationScore:0.00}.";
        }

        var serendipityText = "";
        if (insight.SerendipityOccurred && string.Equals(insight.ToPhase, SimulationPhase.Adaptation, StringComparison.OrdinalIgnoreCase))
        {
            serendipityText = " セレンディピティスコアが閾値を超え、知識再結合が適応相への移行を促した可能性があります。";
        }
        else if (insight.SerendipityOccurred && insight.SerendipityToEmergenceLink)
        {
            serendipityText = " セレンディピティ発生後に強いリンクと知識再結合が重なり、創発相への遷移につながった可能性があります。";
        }
        else if (insight.SerendipityOccurred && insight.KnowledgeReconfigurationScore < 0.40)
        {
            serendipityText = " セレンディピティは発生しましたが、知識再構成が十分ではなく学習相に留まった可能性があります。";
        }

        return $"Step {insight.StepNo} \u3067 {insight.FromPhaseLabel}\u304B\u3089{insight.ToPhaseLabel}\u3078\u79FB\u884C\u3057\u307E\u3057\u305F\u3002\u76F4\u524D{windowSize}step\u3067\u306F{agentText}\u3001{insight.MainActionChange}\u3002{edgeText}\u3001{causeText}\u3002{challengeText}{serendipityText}";
    }

    private static string FormatEdge(TriggerEdgeChange edge)
    {
        return $"{edge.SourceAgentName}\u2192{edge.TargetAgentName} : {edge.BeforeValue:0.00} \u2192 {edge.AfterValue:0.00}";
    }

    private static double RoundDelta(double value)
    {
        return Math.Round(value, 4);
    }

    private static int GetPhaseOrder(string? phase) => phase switch
    {
        "Forming" => 0,
        "Learning" => 1,
        "Adaptation" => 2,
        "Stable" => 3,
        "Emergent" => 4,
        "Silo" => 5,
        "Chaos" => 6,
        "Collapse" => 7,
        _ => 99
    };
}

public sealed class PhaseTransitionStepState
{
    public int StepNo { get; init; }
    public string Phase { get; init; } = "";
    public double ShareInfoRate { get; init; }
    public double AskHelpRate { get; init; }
    public double ProposeIdeaRate { get; init; }
    public double CriticizeRate { get; init; }
    public double WorkAloneRate { get; init; }
    public double SupportOtherRate { get; init; }
    public double WaitRate { get; init; }
    public double OtherRate { get; init; }
    public double AverageTrust { get; init; }
    public double EffectiveNetworkDensity { get; init; }
    public double NewStrongLinkRate { get; init; }
    public int StrongLinkCount { get; init; }
    public int WeakLinkCount { get; init; }
    public int ComponentCount { get; init; }
    public int IsolatedCount { get; init; }
    public bool ChallengeOccurred { get; init; }
    public bool ChallengeActive { get; init; }
    public bool ChallengeResolved { get; init; }
    public double ChallengeResolutionScore { get; init; }
    public double ChallengeGap { get; init; }
    public double KnowledgeReconfigurationScore { get; init; }
    public double ExplorationScore { get; init; }
    public double SerendipityScore { get; init; }
    public bool SerendipityOccurred { get; init; }
    public double KnowledgeRecombinationScore { get; init; }
    public bool SerendipityDrivenReconfiguration { get; init; }
    public bool SerendipityToEmergenceLink { get; init; }
}

public sealed class PhaseTransitionInsight
{
    public int StepNo { get; init; }
    public string FromPhase { get; init; } = "";
    public string ToPhase { get; init; } = "";
    public string FromPhaseLabel { get; init; } = "";
    public string ToPhaseLabel { get; init; } = "";
    public double ShareInfoDelta { get; init; }
    public double AskHelpDelta { get; init; }
    public double ProposeIdeaDelta { get; init; }
    public double CriticizeDelta { get; init; }
    public double WorkAloneDelta { get; init; }
    public double SupportOtherDelta { get; init; }
    public double WaitDelta { get; init; }
    public double OtherDelta { get; init; }
    public double AverageTrustBefore { get; init; }
    public double AverageTrustAfter { get; init; }
    public double AverageTrustDelta { get; init; }
    public double EffectiveDensityBefore { get; init; }
    public double EffectiveDensityAfter { get; init; }
    public double EffectiveDensityDelta { get; init; }
    public int StrongLinksBefore { get; init; }
    public int StrongLinksAfter { get; init; }
    public int StrongLinksDelta { get; init; }
    public int WeakLinksBefore { get; init; }
    public int WeakLinksAfter { get; init; }
    public int WeakLinksDelta { get; init; }
    public int ComponentCountBefore { get; init; }
    public int ComponentCountAfter { get; init; }
    public int ComponentCountDelta { get; init; }
    public int IsolatedCountBefore { get; init; }
    public int IsolatedCountAfter { get; init; }
    public int IsolatedCountDelta { get; init; }
    public bool ChallengeOccurred { get; set; }
    public bool ChallengeActive { get; set; }
    public bool ChallengeResolved { get; set; }
    public double ChallengeResolutionScore { get; set; }
    public double ChallengeGap { get; set; }
    public double KnowledgeReconfigurationScore { get; set; }
    public double ExplorationScore { get; set; }
    public double SerendipityScore { get; set; }
    public bool SerendipityOccurred { get; set; }
    public double KnowledgeRecombinationScore { get; set; }
    public bool SerendipityDrivenReconfiguration { get; set; }
    public bool SerendipityToEmergenceLink { get; set; }
    public string MainIncreasingAction { get; set; } = "";
    public string MainDecreasingAction { get; set; } = "";
    public string WindowDominantAction { get; set; } = "";
    public string MainActionChange { get; set; } = "";
    public string MainNetworkChange { get; set; } = "";
    public string ShortInterpretation { get; set; } = "";
    public List<TriggerAgentInsight> TriggerAgents { get; set; } = [];
    public List<TriggerTrustChangeInsight> TriggerTrustChanges { get; set; } = [];
    public List<TriggerEdgeChange> NewStrongLinks { get; set; } = [];
    public List<TriggerEdgeChange> LostStrongLinks { get; set; } = [];
    public int NewStrongLinkTotalCount { get; set; }
    public int LostStrongLinkTotalCount { get; set; }
    public string ExplainerText { get; set; } = "";
}

public sealed class PhaseTransitionPatternSummary
{
    public string FromPhase { get; init; } = "";
    public string ToPhase { get; init; } = "";
    public int Count { get; init; }
    public double AverageShareInfoDelta { get; init; }
    public double AverageProposeIdeaDelta { get; init; }
    public double AverageWorkAloneDelta { get; init; }
    public double AverageSupportOtherDelta { get; init; }
    public double AverageEffectiveDensityDelta { get; init; }
    public double AverageAverageTrustDelta { get; init; }
    public double AverageNewStrongLinkCount { get; init; }
    public double AverageLostStrongLinkCount { get; init; }
}

public sealed class PhaseTransitionActionRecord
{
    public int StepNo { get; init; }
    public string AgentName { get; init; } = "";
    public string Action { get; init; } = "";
    public string TargetAgentName { get; init; } = "-";
    public double? TrustBefore { get; init; }
    public double? TrustDelta { get; init; }
    public double? TrustAfter { get; init; }
}

public sealed class TriggerAgentInsight
{
    public string AgentName { get; init; } = "";
    public int ActionCount { get; init; }
    public int ShareInfoCount { get; init; }
    public int AskHelpCount { get; init; }
    public int ProposeIdeaCount { get; init; }
    public int SupportOtherCount { get; init; }
    public int CriticizeCount { get; init; }
    public int WorkAloneCount { get; init; }
    public int WaitCount { get; init; }
    public double TrustDeltaSum { get; init; }
    public double TriggerScore { get; init; }
    public string MainAction { get; init; } = "";
}

public sealed class TriggerTrustChangeInsight
{
    public int StepNo { get; init; }
    public string AgentName { get; init; } = "";
    public string Action { get; init; } = "";
    public string TargetAgentName { get; init; } = "-";
    public double? TrustBefore { get; init; }
    public double TrustDelta { get; init; }
    public double? TrustAfter { get; init; }
}

public sealed class TriggerEdgeChange
{
    public string SourceAgentName { get; init; } = "";
    public string TargetAgentName { get; init; } = "";
    public double BeforeValue { get; init; }
    public double AfterValue { get; init; }
}

public sealed class TriggerAgentRankingSummary
{
    public string AgentName { get; init; } = "";
    public int Count { get; init; }
    public double AverageTriggerScore { get; init; }
}
