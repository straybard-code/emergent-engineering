using System.Text.Json;
using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class KnowledgeAnalysisService
{
    public static double CalculateConstructiveCriticismRate(double criticizeRate, double psychologicalSafetyLevel)
    {
        if (psychologicalSafetyLevel >= 0.7)
        {
            return Clamp01(criticizeRate);
        }

        if (psychologicalSafetyLevel >= 0.4)
        {
            return Clamp01(criticizeRate * 0.5);
        }

        return 0;
    }

    public static double CalculateDestructiveCriticismRate(double criticizeRate, double psychologicalSafetyLevel)
    {
        if (psychologicalSafetyLevel >= 0.7)
        {
            return 0;
        }

        if (psychologicalSafetyLevel >= 0.4)
        {
            return Clamp01(criticizeRate * 0.5);
        }

        return Clamp01(criticizeRate);
    }

    public static double CalculatePsychologicalSafetyActionEffect(ActionDistributionSummary actions, double psychologicalSafetyLevel)
    {
        var constructiveCriticismRate = CalculateConstructiveCriticismRate(actions.CriticizeRate, psychologicalSafetyLevel);
        var destructiveCriticismRate = CalculateDestructiveCriticismRate(actions.CriticizeRate, psychologicalSafetyLevel);

        return Math.Round(
            (actions.ShareInfoRate + actions.AskHelpRate + actions.ProposeIdeaRate + actions.SupportOtherRate + constructiveCriticismRate)
            - (actions.WorkAloneRate + actions.WaitRate + destructiveCriticismRate),
            4);
    }

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
        var constructiveCriticizeRate = CalculateConstructiveCriticismRate(actions.CriticizeRate, psychologicalSafetyLevel);
        var rawDelta =
            (actions.ProposeIdeaRate * 0.03)
            + (constructiveCriticizeRate * 0.02)
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
        var constructiveCriticizeRate = CalculateConstructiveCriticismRate(actions.CriticizeRate, psychologicalSafetyLevel);
        var rawScore =
            (knowledgeDiversity * 0.30)
            + (crossDomainExposure * 0.25)
            + (externalShockLevel * 0.20)
            + (challengeLevel * 0.15)
            + (actions.ProposeIdeaRate * 0.15)
            + (constructiveCriticizeRate * 0.10);

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
        var constructiveCriticizeRate = CalculateConstructiveCriticismRate(actions.CriticizeRate, psychologicalSafetyLevel);
        var psychologicalSafetyReconfigurationBonus = psychologicalSafetyLevel * knowledgeRecombinationScore * 0.10;
        var rawScore =
            (knowledgeRewiringScore * 0.35)
            + (knowledgeRecombinationScore * 0.30)
            + (challengeLevel * 0.15)
            + (actions.ProposeIdeaRate * 0.15)
            + (actions.ShareInfoRate * 0.10)
            + (actions.SupportOtherRate * 0.10)
            + (challengeActive ? constructiveCriticizeRate * 0.05 : 0)
            + (psychologicalSafetyLevel >= 0.7 ? constructiveCriticizeRate * Math.Clamp(constructiveCriticismBonus, 0, 1) : 0)
            + psychologicalSafetyReconfigurationBonus;

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

    public static double CalculatePipelineCompletionScore(
        double serendipityScore,
        double knowledgeRecombinationScore,
        double knowledgeReconfigurationScore,
        double learningScore,
        double adaptationScore,
        double emergentScore)
    {
        return Clamp01(Math.Round((
            Clamp01(serendipityScore)
            + Clamp01(knowledgeRecombinationScore)
            + Clamp01(knowledgeReconfigurationScore)
            + Clamp01(learningScore)
            + Clamp01(adaptationScore)
            + Clamp01(emergentScore)) / 6.0, 4));
    }

    public static string DeterminePipelineBottleneck(
        double serendipityScore,
        double knowledgeRecombinationScore,
        double knowledgeReconfigurationScore,
        double learningScore,
        double adaptationScore,
        double emergentScore)
    {
        var ordered = new[]
        {
            new KeyValuePair<string, double>("Serendipity", Clamp01(serendipityScore)),
            new KeyValuePair<string, double>("KnowledgeRecombination", Clamp01(knowledgeRecombinationScore)),
            new KeyValuePair<string, double>("KnowledgeReconfiguration", Clamp01(knowledgeReconfigurationScore)),
            new KeyValuePair<string, double>("Learning", Clamp01(learningScore)),
            new KeyValuePair<string, double>("Adaptation", Clamp01(adaptationScore)),
            new KeyValuePair<string, double>("Emergence", Clamp01(emergentScore))
        };

        return ordered
            .OrderBy(item => item.Value)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .First()
            .Key;
    }

    public static double GetPipelineBottleneckScore(
        string bottleneck,
        double serendipityScore,
        double knowledgeRecombinationScore,
        double knowledgeReconfigurationScore,
        double learningScore,
        double adaptationScore,
        double emergentScore)
    {
        return bottleneck switch
        {
            "Serendipity" => Clamp01(serendipityScore),
            "KnowledgeRecombination" => Clamp01(knowledgeRecombinationScore),
            "KnowledgeReconfiguration" => Clamp01(knowledgeReconfigurationScore),
            "Learning" => Clamp01(learningScore),
            "Adaptation" => Clamp01(adaptationScore),
            "Emergence" => Clamp01(emergentScore),
            _ => 0
        };
    }

    public static string BuildPipelineBottleneckInterpretation(string? bottleneck)
    {
        return bottleneck switch
        {
            "Serendipity" => "偶然の有用結合が不足しており、知識再結合の起点が弱い状態です。",
            "KnowledgeRecombination" => "セレンディピティは発生していますが、知識の組み合わせ直しに十分つながっていません。",
            "KnowledgeReconfiguration" => "知識再結合はありますが、組織全体の知識構造の再編に届いていません。",
            "Learning" => "知識構造は変化していますが、継続的な学習相として安定していません。",
            "Adaptation" => "学習は進んでいますが、環境変化への適応行動に変換されていません。",
            "Emergence" => "前段階はそろっていますが、システム全体の相変化としてはまだ現れていません。",
            _ => "-"
        };
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
            return "Challenge は解決され、適応と再配線を通じて前進しています。";
        }

        if (challengeActive && challengeGap > 0.15)
        {
            return "Challenge は未解決で、ギャップがまだ大きい状態です。";
        }

        if (challengeActive && knowledgeReconfigurationScore >= 0.45)
        {
            return "Challenge の下で、再構成的な適応が進んでいます。";
        }

        if (serendipityOccurred && knowledgeRecombinationScore >= 0.45)
        {
            return "セレンディピティが有効に働き、知識再結合が進んでいます。";
        }

        if (explorationScore >= 0.45 && serendipityScore >= 0.45)
        {
            return "探索行動と偶然の有用結合が並立し、前兆が強まっています。";
        }

        if (externalShockLevel >= 0.25)
        {
            return "外部刺激によりネットワークが揺さぶられています。";
        }

        if (crossDomainExposure >= 0.30 && rewiringScore >= 0.45)
        {
            return "異分野接触と再配線が進み、新しい結合が生まれています。";
        }

        if (workAloneRate >= 0.35 && rewiringScore <= 0.30)
        {
            return "単独作業が多く、組織的な知識再構成が起こりにくい状態です。";
        }

        if (knowledgeStock >= 0.40 && rewiringScore <= 0.40)
        {
            return "知識蓄積は進んでいますが、再配線はまだ弱いです。";
        }

        if (knowledgeDiversity >= 0.45 || rewiringScore >= 0.50)
        {
            return "知識の多様性が確保され、新しい結合の準備が進んでいます。";
        }

        return "知識蓄積と再構成の動きはまだ限定的です。";
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
                AdaptationScore = GetDouble(root, "adaptationScore", 0),
                PhaseDecisionScore = GetDouble(root, "phaseDecisionScore", 0),
                EmergentScore = GetDouble(root, "emergentScore", 0),
                StableScore = GetDouble(root, "stableScore", 0),
                LearningScore = GetDouble(root, "learningScore", 0),
                SiloScore = GetDouble(root, "siloScore", 0),
                ChaosScore = GetDouble(root, "chaosScore", 0),
                CollapseScore = GetDouble(root, "collapseScore", 0),
                SelectedPhase = GetString(root, "selectedPhase", GetString(root, "Phase", "")),
                EmergentCriteriaJson = GetString(root, "emergentCriteriaJson", ""),
                PhaseDecisionReason = GetString(root, "phaseDecisionReason", ""),
                PipelineBottleneck = GetString(
                    root,
                    "pipelineBottleneck",
                    DeterminePipelineBottleneck(
                        GetDouble(root, "serendipityScore", 0),
                        GetDouble(root, "knowledgeRecombinationScore", 0),
                        GetDouble(root, "knowledgeReconfigurationScore", 0),
                        GetDouble(root, "learningScore", 0),
                        GetDouble(root, "adaptationScore", 0),
                        GetDouble(root, "emergentScore", 0))),
                PipelineBottleneckScore = GetDouble(
                    root,
                    "pipelineBottleneckScore",
                    GetPipelineBottleneckScore(
                        GetString(
                            root,
                            "pipelineBottleneck",
                            DeterminePipelineBottleneck(
                                GetDouble(root, "serendipityScore", 0),
                                GetDouble(root, "knowledgeRecombinationScore", 0),
                                GetDouble(root, "knowledgeReconfigurationScore", 0),
                                GetDouble(root, "learningScore", 0),
                                GetDouble(root, "adaptationScore", 0),
                                GetDouble(root, "emergentScore", 0))),
                        GetDouble(root, "serendipityScore", 0),
                        GetDouble(root, "knowledgeRecombinationScore", 0),
                        GetDouble(root, "knowledgeReconfigurationScore", 0),
                        GetDouble(root, "learningScore", 0),
                        GetDouble(root, "adaptationScore", 0),
                        GetDouble(root, "emergentScore", 0))),
                PipelineCompletionScore = GetDouble(
                    root,
                    "pipelineCompletionScore",
                    CalculatePipelineCompletionScore(
                        GetDouble(root, "serendipityScore", 0),
                        GetDouble(root, "knowledgeRecombinationScore", 0),
                        GetDouble(root, "knowledgeReconfigurationScore", 0),
                        GetDouble(root, "learningScore", 0),
                        GetDouble(root, "adaptationScore", 0),
                        GetDouble(root, "emergentScore", 0))),
                PsychologicalSafetyActionEffect = GetDouble(root, "psychologicalSafetyActionEffect", 0, -1, 1),
                ConstructiveCriticismRate = GetDouble(root, "constructiveCriticismRate", 0),
                DestructiveCriticismRate = GetDouble(root, "destructiveCriticismRate", 0),
                PsychologicalSafetyRecombinationBonus = GetDouble(root, "psychologicalSafetyRecombinationBonus", 0),
                PsychologicalSafetySerendipityBonus = GetDouble(root, "psychologicalSafetySerendipityBonus", 0),
                PsychologicalSafetyEmergenceBonus = GetDouble(root, "psychologicalSafetyEmergenceBonus", 0),
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
                AdaptationScore = 0,
                PhaseDecisionScore = 0,
                EmergentScore = 0,
                StableScore = 0,
                LearningScore = 0,
                SiloScore = 0,
                ChaosScore = 0,
                CollapseScore = 0,
                SelectedPhase = "",
                EmergentCriteriaJson = "",
                PhaseDecisionReason = "",
                PipelineBottleneck = "--",
                PipelineBottleneckScore = 0,
                PipelineCompletionScore = 0,
                PsychologicalSafetyActionEffect = 0,
                ConstructiveCriticismRate = 0,
                DestructiveCriticismRate = 0,
                PsychologicalSafetyRecombinationBonus = 0,
                PsychologicalSafetySerendipityBonus = 0,
                PsychologicalSafetyEmergenceBonus = 0,
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
    public double AdaptationScore { get; set; }
    public double PhaseDecisionScore { get; set; }
    public double EmergentScore { get; set; }
    public double StableScore { get; set; }
    public double LearningScore { get; set; }
    public double SiloScore { get; set; }
    public double ChaosScore { get; set; }
    public double CollapseScore { get; set; }
    public string SelectedPhase { get; set; } = "";
    public string EmergentCriteriaJson { get; set; } = "";
    public string PhaseDecisionReason { get; set; } = "";
    public string PipelineBottleneck { get; set; } = "--";
    public double PipelineBottleneckScore { get; set; }
    public double PipelineCompletionScore { get; set; }
    public double PsychologicalSafetyActionEffect { get; set; }
    public double ConstructiveCriticismRate { get; set; }
    public double DestructiveCriticismRate { get; set; }
    public double PsychologicalSafetyRecombinationBonus { get; set; }
    public double PsychologicalSafetySerendipityBonus { get; set; }
    public double PsychologicalSafetyEmergenceBonus { get; set; }
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
