namespace EmergentEngineering.Services;

public static class PrecursorAnalysisService
{
    public static List<PrecursorPoint> BuildPoints(
        IReadOnlyCollection<PhaseTransitionStepState> stepStates,
        double psychologicalSafetyLevel)
    {
        var ordered = stepStates
            .OrderBy(state => state.StepNo)
            .ToList();

        List<PrecursorPoint> points = [];
        for (var index = 0; index < ordered.Count; index++)
        {
            var state = ordered[index];
            var actionConcentration = new[]
            {
                state.ShareInfoRate,
                state.AskHelpRate,
                state.ProposeIdeaRate,
                state.CriticizeRate,
                state.WorkAloneRate,
                state.SupportOtherRate,
                state.WaitRate,
                state.OtherRate
            }.Max();

            var phaseStabilityLocal = CalculatePhaseStabilityLocal(ordered, index);
            var averageTrustNormalized = Math.Clamp((state.AverageTrust + 1.0) / 2.0, 0, 1);
            var componentPenalty = Math.Min(Math.Max(state.ComponentCount - 1, 0), 5) / 5.0;

            var siloRiskScore = Clamp01(
                (state.WorkAloneRate * 0.35)
                + (state.CriticizeRate * 0.20)
                + ((1 - state.ShareInfoRate) * 0.15)
                + ((1 - state.SupportOtherRate) * 0.10)
                + ((1 - state.EffectiveNetworkDensity) * 0.10)
                + (componentPenalty * 0.10)
                + (Math.Max(state.ChallengeGap, 0) * 0.10));

            var stableScore = Clamp01(
                (averageTrustNormalized * 0.30)
                + (state.EffectiveNetworkDensity * 0.30)
                + ((1 - actionConcentration) * 0.20)
                + (phaseStabilityLocal * 0.20));

            var emergentScore = Clamp01(
                (state.ShareInfoRate * 0.20)
                + (state.SupportOtherRate * 0.20)
                + (state.ProposeIdeaRate * 0.20)
                + (state.CriticizeRate * psychologicalSafetyLevel * 0.15)
                + (state.NewStrongLinkRate * 0.15)
                + (state.EffectiveNetworkDensity * 0.10)
                + (state.ChallengeResolutionScore * 0.10)
                + (state.KnowledgeReconfigurationScore * 0.10));

            var forecastPhase = DetermineForecastPhase(siloRiskScore, stableScore, emergentScore);
            var mainSignal = DetermineMainSignal(state, siloRiskScore, stableScore, emergentScore);
            var interpretation = BuildInterpretation(state, siloRiskScore, stableScore, emergentScore);

            points.Add(new PrecursorPoint
            {
                StepNo = state.StepNo,
                Phase = state.Phase,
                ForecastPhase = forecastPhase,
                SiloRiskScore = Math.Round(siloRiskScore, 4),
                StableScore = Math.Round(stableScore, 4),
                EmergentScore = Math.Round(emergentScore, 4),
                MainSignal = mainSignal,
                Interpretation = interpretation
            });
        }

        return points;
    }

    private static double CalculatePhaseStabilityLocal(IReadOnlyList<PhaseTransitionStepState> ordered, int index)
    {
        var start = Math.Max(0, index - 2);
        for (var current = start + 1; current <= index; current++)
        {
            if (!string.Equals(ordered[current - 1].Phase, ordered[current].Phase, StringComparison.OrdinalIgnoreCase))
            {
                return 0.5;
            }
        }

        return 1.0;
    }

    private static string DetermineForecastPhase(double siloRiskScore, double stableScore, double emergentScore)
    {
        if (siloRiskScore >= stableScore && siloRiskScore >= emergentScore)
        {
            return "Silo";
        }

        if (emergentScore >= stableScore)
        {
            return "Emergent";
        }

        return "Stable";
    }

    private static string DetermineMainSignal(
        PhaseTransitionStepState state,
        double siloRiskScore,
        double stableScore,
        double emergentScore)
    {
        if (state.NewStrongLinkRate > 0.05)
        {
            return "\u65B0Strong Link\u5F62\u6210";
        }

        if (state.ChallengeActive && state.KnowledgeReconfigurationScore >= 0.45)
        {
            return "\u9069\u5FDC\u30FB\u518D\u69CB\u6210\u9032\u884C";
        }

        if (state.ChallengeGap > 0.15)
        {
            return "ChallengeGap\u6B8B\u5B58";
        }

        if (siloRiskScore >= stableScore && siloRiskScore >= emergentScore)
        {
            if (state.WorkAloneRate >= 0.35)
            {
                return "\u5358\u72EC\u4F5C\u696D\u512A\u52E2";
            }

            if (state.ShareInfoRate <= 0.15)
            {
                return "\u60C5\u5831\u5171\u6709\u4F4E\u4E0B";
            }

            if (state.EffectiveNetworkDensity <= 0.40)
            {
                return "\u5B9F\u52B9\u5BC6\u5EA6\u4F4E\u4E0B";
            }
        }

        if (emergentScore >= siloRiskScore && emergentScore >= stableScore)
        {
            if (state.SupportOtherRate >= 0.20)
            {
                return "\u652F\u63F4\u5897\u52A0";
            }

            if (state.ProposeIdeaRate >= 0.20)
            {
                return "\u30A2\u30A4\u30C7\u30A2\u63D0\u6848\u5897\u52A0";
            }
        }

        return "\u884C\u52D5\u5206\u5E03\u5B89\u5B9A";
    }

    private static string BuildInterpretation(
        PhaseTransitionStepState state,
        double siloRiskScore,
        double stableScore,
        double emergentScore)
    {
        if (siloRiskScore >= stableScore && siloRiskScore >= emergentScore)
        {
            return "\u5358\u72EC\u4F5C\u696D\u304C\u591A\u304F\u3001\u60C5\u5831\u5171\u6709\u304C\u5C11\u306A\u3044\u305F\u3081\u3001\u30B5\u30A4\u30ED\u5316\u30EA\u30B9\u30AF\u304C\u9AD8\u307E\u3063\u3066\u3044\u307E\u3059\u3002";
        }

        if (state.ChallengeActive && state.KnowledgeReconfigurationScore >= 0.45)
        {
            return "Challenge\u306B\u5BFE\u3057\u3066\u77E5\u8B58\u518D\u69CB\u6210\u304C\u9032\u307F\u3001\u9069\u5FDC\u76F8\u3078\u306E\u79FB\u884C\u53EF\u80FD\u6027\u304C\u3042\u308A\u307E\u3059\u3002";
        }

        if (emergentScore >= siloRiskScore && emergentScore >= stableScore)
        {
            return "\u60C5\u5831\u5171\u6709\u30FB\u652F\u63F4\u30FB\u63D0\u6848\u304C\u4E26\u7ACB\u3057\u3066\u304A\u308A\u3001\u5275\u767A\u76F8\u3078\u306E\u79FB\u884C\u53EF\u80FD\u6027\u304C\u3042\u308A\u307E\u3059\u3002";
        }

        return "\u4FE1\u983C\u30CD\u30C3\u30C8\u30EF\u30FC\u30AF\u306F\u7DAD\u6301\u3055\u308C\u3001\u884C\u52D5\u5206\u5E03\u3082\u5B89\u5B9A\u3057\u3066\u3044\u307E\u3059\u3002";
    }

    private static double Clamp01(double value)
    {
        return Math.Clamp(value, 0, 1);
    }
}

public sealed class PrecursorPoint
{
    public int StepNo { get; init; }
    public string Phase { get; init; } = "";
    public string ForecastPhase { get; init; } = "";
    public double SiloRiskScore { get; init; }
    public double StableScore { get; init; }
    public double EmergentScore { get; init; }
    public string MainSignal { get; init; } = "";
    public string Interpretation { get; init; } = "";
}

public sealed class ExperimentPrecursorPoint
{
    public int StepNo { get; init; }
    public int RunCount { get; init; }
    public double AverageSiloRiskScore { get; init; }
    public double AverageStableScore { get; init; }
    public double AverageEmergentScore { get; init; }
    public string MainSignal { get; init; } = "";
}
