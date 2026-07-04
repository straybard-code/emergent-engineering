using System.Text.Json;
using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class KnowledgeAnalysisService
{
    public static double CalculateKnowledgeStockDelta(ActionDistributionSummary actions)
    {
        return Math.Round(
            (actions.ShareInfoRate * 0.03)
            + (actions.SupportOtherRate * 0.02)
            + (actions.AskHelpRate * 0.01)
            - (actions.WorkAloneRate * 0.01),
            4);
    }

    public static double CalculateKnowledgeDiversityDelta(
        ActionDistributionSummary actions,
        double psychologicalSafetyLevel,
        double crossDomainExposure,
        double externalShockLevel,
        double rewiringSensitivity)
    {
        var rawDelta =
            (actions.ProposeIdeaRate * 0.03)
            + (actions.CriticizeRate * psychologicalSafetyLevel * 0.02)
            + (crossDomainExposure * 0.01)
            + (externalShockLevel * 0.01);

        var sensitivityScale = 0.5 + (Math.Clamp(rewiringSensitivity, 0, 1) * 0.5);
        return Math.Round(rawDelta * sensitivityScale, 4);
    }

    public static double CalculateKnowledgeRewiringScore(
        double knowledgeDiversity,
        double crossDomainExposure,
        double externalShockLevel,
        double challengeLevel,
        ActionDistributionSummary actions,
        double psychologicalSafetyLevel,
        double rewiringSensitivity)
    {
        var rawScore =
            (knowledgeDiversity * 0.30)
            + (crossDomainExposure * 0.25)
            + (externalShockLevel * 0.20)
            + (challengeLevel * 0.15)
            + (actions.ProposeIdeaRate * 0.15)
            + (actions.CriticizeRate * psychologicalSafetyLevel * 0.10);

        var sensitivityScale = 0.5 + (Math.Clamp(rewiringSensitivity, 0, 1) * 0.5);
        return Clamp01(Math.Round(rawScore * sensitivityScale, 4));
    }

    public static double CalculateKnowledgeReconfigurationScore(
        double knowledgeRewiringScore,
        double challengeLevel,
        ActionDistributionSummary actions,
        double psychologicalSafetyLevel,
        bool challengeActive,
        double knowledgeRecombinationScore,
        double constructiveCriticismBonus = 0)
    {
        var rawScore =
            (knowledgeRewiringScore * 0.35)
            + (knowledgeRecombinationScore * 0.30)
            + (challengeLevel * 0.15)
            + (actions.ProposeIdeaRate * 0.15)
            + (actions.ShareInfoRate * 0.10)
            + (actions.SupportOtherRate * 0.10)
            + (challengeActive ? actions.CriticizeRate * psychologicalSafetyLevel * 0.05 : 0)
            + (psychologicalSafetyLevel >= 0.7 ? actions.CriticizeRate * Math.Clamp(constructiveCriticismBonus, 0, 1) : 0);

        return Clamp01(Math.Round(rawScore, 4));
    }

    public static double CalculateChallengeResolutionScore(
        double knowledgeDiversity,
        double crossDomainExposure,
        double knowledgeReconfigurationScore,
        ActionDistributionSummary actions)
    {
        return Clamp01(Math.Round(
            (knowledgeDiversity * 0.30)
            + (crossDomainExposure * 0.20)
            + (knowledgeReconfigurationScore * 0.30)
            + (actions.ShareInfoRate * 0.10)
            + (actions.SupportOtherRate * 0.10),
            4));
    }

    public static double CalculateChallengeRequirementAverage(
        double requiredKnowledgeDiversity,
        double requiredCrossDomainExposure,
        double requiredRewiringScore)
    {
        return Math.Round((
            Math.Clamp(requiredKnowledgeDiversity, 0, 1)
            + Math.Clamp(requiredCrossDomainExposure, 0, 1)
            + Math.Clamp(requiredRewiringScore, 0, 1)) / 3.0, 4);
    }

    public static string BuildInterpretation(
        double knowledgeStock,
        double knowledgeDiversity,
        double externalShockLevel,
        double crossDomainExposure,
        double rewiringScore,
        double knowledgeReconfigurationScore,
        double explorationScore,
        double serendipityScore,
        double knowledgeRecombinationScore,
        bool serendipityOccurred,
        bool challengeActive,
        bool challengeResolved,
        double challengeGap,
        double workAloneRate,
        double constructiveCriticismBonus = 0)
    {
        if (challengeResolved)
        {
            return "Challenge が解決され、組織の知識再構成が前進しています。";
        }

        if (challengeActive && challengeGap > 0.15)
        {
            return "Challenge が未解決で、適応ギャップがまだ大きく残っています。";
        }

        if (challengeActive && knowledgeReconfigurationScore >= 0.45)
        {
            return "Challenge 圧力により、越境的な適応と知識再構成が進んでいます。";
        }

        if (serendipityOccurred && knowledgeRecombinationScore >= 0.45)
        {
            return "セレンディピティが閾値を超え、知識再結合が加速しています。";
        }

        if (explorationScore >= 0.45 && serendipityScore >= 0.45)
        {
            return "探索行動と異分野接触が増え、有用な知識結合の可能性が高まっています。";
        }

        if (externalShockLevel >= 0.25)
        {
            return "外部刺激により知識ネットワークが揺さぶられています。";
        }

        if (crossDomainExposure >= 0.30 && rewiringScore >= 0.45)
        {
            return "異分野接触と提案行動が、再配線の可能性を高めています。";
        }

        if (workAloneRate >= 0.35 && rewiringScore <= 0.30)
        {
            return "単独作業が優勢で、知識が組織的に共有されにくい状態です。";
        }

        if (knowledgeStock >= 0.40 && rewiringScore <= 0.40)
        {
            return "知識蓄積が進み、組織学習が安定しています。";
        }

        if (knowledgeDiversity >= 0.45 || rewiringScore >= 0.50)
        {
            return "新しい知識結合が生まれ、再配線が活発化しています。";
        }

        return "知識蓄積と知識多様性が緩やかに増加しています。";
    }

    public static List<KnowledgeTimelinePoint> BuildTimeline(IEnumerable<SimulationStep> steps)
    {
        return steps
            .OrderBy(step => step.StepNo)
            .Select(ParseState)
            .Where(point => point is not null)
            .Cast<KnowledgeTimelinePoint>()
            .ToList();
    }

    private static KnowledgeTimelinePoint? ParseState(SimulationStep step)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(step.StateJson) ? "{}" : step.StateJson);
            var root = document.RootElement;

            return new KnowledgeTimelinePoint
            {
                StepNo = step.StepNo,
                Phase = GetString(root, "Phase", GetString(root, "phase", "")),
                KnowledgeStock = GetDouble(root, "knowledgeStock", KnowledgeDefaults.Stock),
                KnowledgeDiversity = GetDouble(root, "knowledgeDiversity", KnowledgeDefaults.Diversity),
                ExternalShockLevel = GetDouble(root, "externalShockLevel", KnowledgeDefaults.ExternalShockLevel),
                CrossDomainExposure = GetDouble(root, "crossDomainExposure", KnowledgeDefaults.CrossDomainExposure),
                KnowledgeRewiringScore = GetDouble(root, "knowledgeRewiringScore", 0),
                ExplorationScore = GetDouble(root, "explorationScore", 0),
                SerendipityScore = GetDouble(root, "serendipityScore", 0),
                SerendipityOccurred = GetBool(root, "serendipityOccurred"),
                KnowledgeRecombinationScore = GetDouble(root, "knowledgeRecombinationScore", 0),
                KnowledgeReconfigurationScore = GetDouble(root, "knowledgeReconfigurationScore", 0),
                AverageTrustGrowthRateEffective = GetDouble(root, "averageTrustGrowthRateEffective", 0),
                AverageTrustDecayApplied = GetDouble(root, "averageTrustDecayApplied", 0),
                TrustCapacityPenaltyAppliedCount = GetInt(root, "trustCapacityPenaltyAppliedCount", 0),
                TrustCapacityPenaltyTotal = GetDouble(root, "trustCapacityPenaltyTotal", 0),
                AverageTrustSaturationEffect = GetDouble(root, "averageTrustSaturationEffect", 0),
                SerendipityDrivenReconfiguration = GetBool(root, "serendipityDrivenReconfiguration"),
                SerendipityToEmergenceLink = GetBool(root, "serendipityToEmergenceLink"),
                ShockOccurred = GetBool(root, "shockOccurred"),
                ShockType = GetString(root, "shockType", ShockTypes.None),
                ChallengeOccurred = GetBool(root, "challengeOccurred"),
                ChallengeActive = GetBool(root, "challengeActive"),
                ChallengeResolved = GetBool(root, "challengeResolved"),
                ChallengeType = GetString(root, "challengeType", ChallengeTypes.None),
                ChallengeResolutionScore = GetDouble(root, "challengeResolutionScore", 0),
                ChallengeGap = GetDouble(root, "challengeGap", 0, -1, 1),
                Interpretation = GetString(root, "knowledgeInterpretation", "")
            };
        }
        catch
        {
            return new KnowledgeTimelinePoint
            {
                StepNo = step.StepNo,
                Phase = "",
                KnowledgeStock = KnowledgeDefaults.Stock,
                KnowledgeDiversity = KnowledgeDefaults.Diversity,
                ExternalShockLevel = KnowledgeDefaults.ExternalShockLevel,
                CrossDomainExposure = KnowledgeDefaults.CrossDomainExposure,
                KnowledgeRewiringScore = 0,
                ExplorationScore = 0,
                SerendipityScore = 0,
                SerendipityOccurred = false,
                KnowledgeRecombinationScore = 0,
                KnowledgeReconfigurationScore = 0,
                AverageTrustGrowthRateEffective = 0,
                AverageTrustDecayApplied = 0,
                TrustCapacityPenaltyAppliedCount = 0,
                TrustCapacityPenaltyTotal = 0,
                AverageTrustSaturationEffect = 0,
                SerendipityDrivenReconfiguration = false,
                SerendipityToEmergenceLink = false,
                ShockOccurred = false,
                ShockType = ShockTypes.None,
                ChallengeOccurred = false,
                ChallengeActive = false,
                ChallengeResolved = false,
                ChallengeType = ChallengeTypes.None,
                ChallengeResolutionScore = 0,
                ChallengeGap = 0,
                Interpretation = ""
            };
        }
    }

    private static double GetDouble(JsonElement root, string propertyName, double fallback, double min = 0, double max = 1)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return fallback;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value)
            ? Math.Clamp(value, min, max)
            : fallback;
    }

    private static int GetInt(JsonElement root, string propertyName, int fallback, int min = int.MinValue, int max = int.MaxValue)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return fallback;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
        {
            return Math.Clamp(value, min, max);
        }

        return fallback;
    }

    private static bool GetBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            && property.GetBoolean();
    }

    private static string GetString(JsonElement root, string propertyName, string fallback)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;
    }

    private static double Clamp01(double value)
    {
        return Math.Clamp(value, 0, 1);
    }
}

public sealed class KnowledgeTimelinePoint
{
    public int StepNo { get; set; }
    public string Phase { get; set; } = "";
    public double KnowledgeStock { get; set; }
    public double KnowledgeDiversity { get; set; }
    public double ExternalShockLevel { get; set; }
    public double CrossDomainExposure { get; set; }
    public double KnowledgeRewiringScore { get; set; }
    public double ExplorationScore { get; set; }
    public double SerendipityScore { get; set; }
    public bool SerendipityOccurred { get; set; }
    public double KnowledgeRecombinationScore { get; set; }
    public double KnowledgeReconfigurationScore { get; set; }
    public double AverageTrustGrowthRateEffective { get; set; }
    public double AverageTrustDecayApplied { get; set; }
    public int TrustCapacityPenaltyAppliedCount { get; set; }
    public double TrustCapacityPenaltyTotal { get; set; }
    public double AverageTrustSaturationEffect { get; set; }
    public bool SerendipityDrivenReconfiguration { get; set; }
    public bool SerendipityToEmergenceLink { get; set; }
    public bool ShockOccurred { get; set; }
    public string ShockType { get; set; } = ShockTypes.None;
    public bool ChallengeOccurred { get; set; }
    public bool ChallengeActive { get; set; }
    public bool ChallengeResolved { get; set; }
    public string ChallengeType { get; set; } = ChallengeTypes.None;
    public double ChallengeResolutionScore { get; set; }
    public double ChallengeGap { get; set; }
    public string Interpretation { get; set; } = "";
}
