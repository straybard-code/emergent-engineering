using System.Globalization;
using System.Text;
using EmergentEngineering.Models;
using EmergentEngineering.Pages.ParameterSweeps;
using EmergentEngineering.Pages.PhaseDiagrams;

namespace EmergentEngineering.Services;

public sealed class ExperimentInterpretationService
{
    public ParameterSweepInterpretationResult BuildParameterSweepInterpretation(IReadOnlyList<ParameterSweepAnalysisPoint> points)
    {
        var ordered = points.OrderBy(item => item.ParameterValue).ToList();
        if (ordered.Count == 0)
        {
            return ParameterSweepInterpretationResult.Empty;
        }

        var maxEmergentRate = ordered.Max(item => item.EmergentRate);
        var maxStableRate = ordered.Max(item => item.StableRate);
        var maxLearningRate = ordered.Max(item => item.LearningRate);
        var maxAverageTrust = ordered.Max(item => item.AverageTrust);
        var maxEffectiveDensity = ordered.Max(item => item.EffectiveDensity);
        var maxSerendipityRate = ordered.Max(item => item.SerendipityRate);
        var maxKnowledgeReconfiguration = ordered.Max(item => item.AverageKnowledgeReconfigurationScore);
        var maxEmergentScore = ordered.Max(item => item.AverageEmergentScore);
        var maxIntellectualRespect = ordered.Max(item => item.AverageIntellectualRespect);
        var maxIdeaAcceptance = ordered.Max(item => item.IdeaAcceptanceScore);
        var maxMutualMentorship = ordered.Max(item => item.MutualMentorshipScore);
        var maxIntellectualRespectReconfiguration = ordered.Max(item => item.AverageIntellectualRespectReconfigurationComponent);
        var maxIntellectualRespectSerendipity = ordered.Max(item => item.AverageIntellectualRespectSerendipityComponent);
        var maxIntellectualRespectEmergence = ordered.Max(item => item.AverageIntellectualRespectEmergenceComponent);

        var bestEmergent = SelectBest(ordered, item => item.EmergentRate);
        var bestStable = SelectBest(ordered, item => item.StableRate);
        var bestLearning = SelectBest(ordered, item => item.LearningRate);
        var bestTrust = SelectBest(ordered, item => item.AverageTrust);
        var bestDensity = SelectBest(ordered, item => item.EffectiveDensity);
        var bestSerendipity = SelectBest(ordered, item => item.SerendipityRate);
        var bestReconfiguration = SelectBest(ordered, item => item.AverageKnowledgeReconfigurationScore);
        var bestIntellectualRespect = SelectBest(ordered, item => item.AverageIntellectualRespect);
        var bestIdeaAcceptance = SelectBest(ordered, item => item.IdeaAcceptanceScore);
        var bestMutualMentorship = SelectBest(ordered, item => item.MutualMentorshipScore);
        var bestIntellectualReconfiguration = SelectBest(ordered, item => item.AverageIntellectualRespectReconfigurationComponent);
        var bestIntellectualSerendipity = SelectBest(ordered, item => item.AverageIntellectualRespectSerendipityComponent);
        var bestIntellectualEmergence = SelectBest(ordered, item => item.AverageIntellectualRespectEmergenceComponent);
        var bestTrustJump = SelectBestDelta(ordered, item => item.DeltaAverageTrust);
        var bestEmergentJump = SelectBestDelta(ordered, item => item.DeltaEmergentRate);

        var recommendedRegion = BuildRecommendedParameterRegion(
            ordered,
            maxEmergentRate,
            maxEmergentScore,
            maxEffectiveDensity,
            maxKnowledgeReconfiguration);

        return new ParameterSweepInterpretationResult
        {
            SummaryCards =
            [
                new() { Label = "最大創発率のParameterValue", Value = FormatPointValue(bestEmergent, item => item.EmergentRate) },
                new() { Label = "最大安定率のParameterValue", Value = FormatPointValue(bestStable, item => item.StableRate) },
                new() { Label = "最大学習率のParameterValue", Value = FormatPointValue(bestLearning, item => item.LearningRate) },
                new() { Label = "最大平均信頼度のParameterValue", Value = FormatPointValue(bestTrust, item => item.AverageTrust) },
                new() { Label = "最大実効密度のParameterValue", Value = FormatPointValue(bestDensity, item => item.EffectiveDensity) },
                new() { Label = "最大セレンディピティ率のParameterValue", Value = FormatPointValue(bestSerendipity, item => item.SerendipityRate) },
                new() { Label = "最大知識再構成スコアのParameterValue", Value = FormatPointValue(bestReconfiguration, item => item.AverageKnowledgeReconfigurationScore) },
                new() { Label = "最大知的敬意のParameterValue", Value = FormatPointValue(bestIntellectualRespect, item => item.AverageIntellectualRespect) },
                new() { Label = "最大アイデア受容スコアのParameterValue", Value = FormatPointValue(bestIdeaAcceptance, item => item.IdeaAcceptanceScore) },
                new() { Label = "最大相互師匠スコアのParameterValue", Value = FormatPointValue(bestMutualMentorship, item => item.MutualMentorshipScore) },
                new() { Label = "最大知的敬意再構成のParameterValue", Value = FormatPointValue(bestIntellectualReconfiguration, item => item.AverageIntellectualRespectReconfigurationComponent) },
                new() { Label = "最大知的敬意セレンディピティのParameterValue", Value = FormatPointValue(bestIntellectualSerendipity, item => item.AverageIntellectualRespectSerendipityComponent) },
                new() { Label = "最大知的敬意創発のParameterValue", Value = FormatPointValue(bestIntellectualEmergence, item => item.AverageIntellectualRespectEmergenceComponent) },
                new() { Label = "最大信頼ジャンプ区間", Value = FormatInterval(bestTrustJump?.PreviousParameterValue, bestTrustJump?.ParameterValue, bestTrustJump?.DeltaAverageTrust) },
                new() { Label = "最大創発ジャンプ区間", Value = FormatInterval(bestEmergentJump?.PreviousParameterValue, bestEmergentJump?.ParameterValue, bestEmergentJump?.DeltaEmergentRate) },
                new() { Label = "推奨パラメータ領域", Value = recommendedRegion }
            ],
            RecommendedRegion = recommendedRegion,
            AutoComment = BuildParameterSweepComment(
                maxEmergentRate,
                maxEmergentScore,
                maxAverageTrust,
                maxEffectiveDensity,
                maxSerendipityRate,
                maxKnowledgeReconfiguration,
                recommendedRegion),
            PrimaryConclusion = BuildParameterSweepConclusion(ordered, maxEmergentRate, maxEmergentScore, maxEffectiveDensity),
            TransitionCandidates = BuildParameterSweepTransitionCandidates(ordered),
            BottleneckRows = BuildParameterSweepBottlenecks(ordered)
        };
    }

    public PhaseDiagramInterpretationResult BuildPhaseDiagramInterpretation(
        IReadOnlyList<PhaseDiagramPoint> points,
        IReadOnlyDictionary<int, PhaseDiagramExperimentSummary>? experimentSummariesByExperimentId = null)
    {
        var ordered = points.OrderBy(item => item.YValue).ThenBy(item => item.XValue).ToList();
        if (ordered.Count == 0)
        {
            return PhaseDiagramInterpretationResult.Empty;
        }

        var maxEmergent = SelectBest(ordered, item => item.EmergentRate);
        var maxStable = SelectBest(ordered, item => item.StableRate);
        var maxLearning = SelectBest(ordered, item => item.LearningRate);
        var maxTrust = SelectBest(ordered, item => item.AverageTrust);
        var maxDensity = SelectBest(ordered, item => item.AverageEffectiveDensity);
        var maxSerendipity = SelectBest(ordered, item => item.AverageSerendipityRate);
        var maxReconfiguration = SelectBest(ordered, item => item.AverageKnowledgeReconfigurationScore);
        var maxPipelineCompletion = ordered.Max(item => item.AveragePipelineCompletionScore);
        var maxCompositeScore = ordered.Max(item =>
            (item.AveragePipelineCompletionScore + item.AverageEffectiveDensity + item.AverageKnowledgeReconfigurationScore) / 3.0);
        var experimentSummaryValues = experimentSummariesByExperimentId?.Values.ToList() ?? [];
        var averageIntellectualRespect = experimentSummaryValues.Count == 0 ? 0 : Math.Round(experimentSummaryValues.Average(item => item.AverageIntellectualRespect), 4);
        var averageIntellectualRespectDensity = experimentSummaryValues.Count == 0 ? 0 : Math.Round(experimentSummaryValues.Average(item => item.IntellectualRespectDensity), 4);
        var averageIntellectualRespectDiversityIndex = experimentSummaryValues.Count == 0 ? 0 : Math.Round(experimentSummaryValues.Average(item => item.IntellectualRespectDiversityIndex), 4);
        var averageIdeaAcceptanceScore = experimentSummaryValues.Count == 0 ? 0 : Math.Round(experimentSummaryValues.Average(item => item.IdeaAcceptanceScore), 4);
        var averageMutualMentorshipScore = experimentSummaryValues.Count == 0 ? 0 : Math.Round(experimentSummaryValues.Average(item => item.MutualMentorshipScore), 4);
        var averageIntellectualRespectReconfiguration = experimentSummaryValues.Count == 0 ? 0 : Math.Round(experimentSummaryValues.Average(item => item.AverageIntellectualRespectReconfigurationComponent), 4);
        var averageIntellectualRespectSerendipity = experimentSummaryValues.Count == 0 ? 0 : Math.Round(experimentSummaryValues.Average(item => item.AverageIntellectualRespectSerendipityComponent), 4);
        var averageIntellectualRespectEmergence = experimentSummaryValues.Count == 0 ? 0 : Math.Round(experimentSummaryValues.Average(item => item.AverageIntellectualRespectEmergenceComponent), 4);

        var recommendedRegion = BuildRecommendedPhaseRegion(
            ordered,
            maxEmergent?.EmergentRate ?? 0,
            maxPipelineCompletion,
            maxCompositeScore);
        var dangerRegion = BuildDangerRegion(ordered, experimentSummariesByExperimentId);

        return new PhaseDiagramInterpretationResult
        {
            SummaryCards =
            [
                new() { Label = "最大創発セル", Value = FormatPointCell(maxEmergent, item => item.EmergentRate) },
                new() { Label = "最大安定セル", Value = FormatPointCell(maxStable, item => item.StableRate) },
                new() { Label = "最大学習セル", Value = FormatPointCell(maxLearning, item => item.LearningRate) },
                new() { Label = "最大平均信頼セル", Value = FormatPointCell(maxTrust, item => item.AverageTrust) },
                new() { Label = "最大実効密度セル", Value = FormatPointCell(maxDensity, item => item.AverageEffectiveDensity) },
                new() { Label = "最大セレンディピティセル", Value = FormatPointCell(maxSerendipity, item => item.AverageSerendipityRate) },
                new() { Label = "最大知識再構成セル", Value = FormatPointCell(maxReconfiguration, item => item.AverageKnowledgeReconfigurationScore) },
                new() { Label = "平均知的敬意", Value = averageIntellectualRespect.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "平均知的敬意密度", Value = averageIntellectualRespectDensity.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "平均知的敬意多様性", Value = averageIntellectualRespectDiversityIndex.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "平均アイデア受容", Value = averageIdeaAcceptanceScore.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "平均相互師匠スコア", Value = averageMutualMentorshipScore.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "平均知的敬意再構成", Value = averageIntellectualRespectReconfiguration.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "平均知的敬意セレンディピティ", Value = averageIntellectualRespectSerendipity.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "平均知的敬意創発", Value = averageIntellectualRespectEmergence.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "推奨領域", Value = recommendedRegion },
                new() { Label = "危険領域", Value = dangerRegion }
            ],
            RecommendedRegion = recommendedRegion,
            DangerRegion = dangerRegion,
            AutoComment = BuildPhaseDiagramComment(
                maxEmergent?.EmergentRate ?? 0,
                maxPipelineCompletion,
                maxTrust?.AverageTrust ?? 0,
                maxDensity?.AverageEffectiveDensity ?? 0,
                maxSerendipity?.AverageSerendipityRate ?? 0,
                maxReconfiguration?.AverageKnowledgeReconfigurationScore ?? 0,
                dangerRegion),
            TransitionCandidates = BuildPhaseDiagramTransitionCandidates(ordered),
            BottleneckRows = BuildPhaseDiagramBottlenecks(ordered, experimentSummariesByExperimentId)
        };
    }

    public string BuildParameterSweepCsv(IReadOnlyList<ParameterSweepAnalysisPoint> points)
    {
        var ordered = points.OrderBy(item => item.ParameterValue).ToList();
        var headers =
            new[]
            {
                "ParameterValue",
                "AverageTrust",
                "EffectiveDensity",
                "StrongLinks",
                "WeakLinks",
                "EmergentRate",
                "StableRate",
                "LearningRate",
                "SiloRate",
                "SerendipityRate",
                "SerendipityToEmergenceRate",
                "AverageKnowledgeDiversity",
                "AverageKnowledgeRecombinationScore",
                "AverageKnowledgeReconfigurationScore",
                "AverageIntellectualRespect",
                "IntellectualRespectDensity",
                "IntellectualRespectDiversityIndex",
                "IdeaAcceptanceScore",
                "AverageEmergentScore",
                "AverageStableScore",
                "AverageLearningScore",
                "AverageIntellectualRespectReconfigurationComponent",
                "AverageIntellectualRespectSerendipityComponent",
                "AverageIntellectualRespectEmergenceComponent",
                "MutualMentorshipScore",
                "LearnedFromUnexpectedAgentCount"
            };

        var rows = ordered.Select(point => new[]
        {
            point.ParameterValue.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageTrust.ToString("0.000", CultureInfo.InvariantCulture),
            point.EffectiveDensity.ToString("0.000", CultureInfo.InvariantCulture),
            point.StrongLinks.ToString("0.000", CultureInfo.InvariantCulture),
            point.WeakLinks.ToString("0.000", CultureInfo.InvariantCulture),
            point.EmergentRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.StableRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.LearningRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.SiloRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.SerendipityRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.SerendipityToEmergenceRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageKnowledgeDiversity.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageKnowledgeRecombinationScore.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageKnowledgeReconfigurationScore.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageIntellectualRespect.ToString("0.000", CultureInfo.InvariantCulture),
            point.IntellectualRespectDensity.ToString("0.000", CultureInfo.InvariantCulture),
            point.IntellectualRespectDiversityIndex.ToString("0.000", CultureInfo.InvariantCulture),
            point.IdeaAcceptanceScore.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageEmergentScore.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageStableScore.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageLearningScore.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageIntellectualRespectReconfigurationComponent.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageIntellectualRespectSerendipityComponent.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageIntellectualRespectEmergenceComponent.ToString("0.000", CultureInfo.InvariantCulture),
            point.MutualMentorshipScore.ToString("0.000", CultureInfo.InvariantCulture),
            point.LearnedFromUnexpectedAgentCount.ToString(CultureInfo.InvariantCulture)
        });

        return BuildCsv(headers, rows);
    }

    public string BuildPhaseDiagramCsv(
        IReadOnlyList<PhaseDiagramPoint> points,
        string xParameterName,
        string yParameterName)
    {
        var ordered = points.OrderBy(item => item.YValue).ThenBy(item => item.XValue).ToList();
        var headers =
            new[]
            {
                "XParameterName",
                "XValue",
                "YParameterName",
                "YValue",
                "AverageTrust",
                "EffectiveDensity",
                "EmergentRate",
                "StableRate",
                "LearningRate",
                "SiloRate",
                "SerendipityRate",
                "AverageKnowledgeReconfigurationScore",
                "AverageEmergentScore",
                "DominantPhase"
            };

        var rows = ordered.Select(point => new[]
        {
            xParameterName,
            point.XValue.ToString("0.000", CultureInfo.InvariantCulture),
            yParameterName,
            point.YValue.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageTrust.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageEffectiveDensity.ToString("0.000", CultureInfo.InvariantCulture),
            point.EmergentRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.StableRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.LearningRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.SiloRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageSerendipityRate.ToString("0.000", CultureInfo.InvariantCulture),
            point.AverageKnowledgeReconfigurationScore.ToString("0.000", CultureInfo.InvariantCulture),
            point.AveragePipelineCompletionScore.ToString("0.000", CultureInfo.InvariantCulture),
            point.DominantPhase
        });

        return BuildCsv(headers, rows);
    }

    private static List<TransitionCandidateRow> BuildParameterSweepTransitionCandidates(IReadOnlyList<ParameterSweepAnalysisPoint> ordered)
    {
        List<TransitionCandidateRow> rows = [];
        for (var index = 1; index < ordered.Count; index++)
        {
            var previous = ordered[index - 1];
            var current = ordered[index];
            var interval = $"{previous.ParameterValue:0.000} -> {current.ParameterValue:0.000}";

            AddTransitionCandidate(rows, interval, "信頼ジャンプ", current.DeltaAverageTrust, "信頼形成が急に進んでいます。");
            AddTransitionCandidate(rows, interval, "密度ジャンプ", current.DeltaEffectiveDensity, "実効ネットワーク密度が急増しています。");
            AddTransitionCandidate(rows, interval, "創発ジャンプ", current.DeltaEmergentRate, "創発率が急に高まっています。");
            AddTransitionCandidate(rows, interval, "安定化ジャンプ", Math.Round(current.StableRate - previous.StableRate, 3), "安定相が強まっています。");
            AddTransitionCandidate(rows, interval, "セレンディピティジャンプ", current.DeltaSerendipityRate, "セレンディピティが増えています。");
            AddTransitionCandidate(rows, interval, "知識再構成ジャンプ", current.DeltaKnowledgeReconfigurationScore, "知識再構成が進んでいます。");
        }

        return rows;
    }

    private static List<TransitionCandidateRow> BuildPhaseDiagramTransitionCandidates(IReadOnlyList<PhaseDiagramPoint> ordered)
    {
        if (ordered.Count == 0)
        {
            return [];
        }

        var pointMap = ordered.ToDictionary(item => GetGridKey(item.XValue, item.YValue));
        var xValues = ordered.Select(item => item.XValue).Distinct().OrderBy(item => item).ToList();
        var yValues = ordered.Select(item => item.YValue).Distinct().OrderBy(item => item).ToList();
        List<TransitionCandidateRow> rows = [];

        for (var yIndex = 0; yIndex < yValues.Count; yIndex++)
        {
            var yValue = yValues[yIndex];
            for (var xIndex = 0; xIndex < xValues.Count; xIndex++)
            {
                var xValue = xValues[xIndex];
                if (!pointMap.TryGetValue(GetGridKey(xValue, yValue), out var current))
                {
                    continue;
                }

                if (xIndex + 1 < xValues.Count && pointMap.TryGetValue(GetGridKey(xValues[xIndex + 1], yValue), out var right))
                {
                    AddGridTransitionCandidates(rows, current, right, "Horizontal");
                }

                if (yIndex + 1 < yValues.Count && pointMap.TryGetValue(GetGridKey(xValue, yValues[yIndex + 1]), out var down))
                {
                    AddGridTransitionCandidates(rows, current, down, "Vertical");
                }
            }
        }

        return rows;
    }

    private static void AddGridTransitionCandidates(
        ICollection<TransitionCandidateRow> rows,
        PhaseDiagramPoint current,
        PhaseDiagramPoint neighbor,
        string direction)
    {
        var interval = $"{current.XValue:0.000},{current.YValue:0.000} -> {neighbor.XValue:0.000},{neighbor.YValue:0.000} ({direction})";

        AddTransitionCandidate(rows, interval, "信頼ジャンプ", neighbor.AverageTrust - current.AverageTrust, "信頼形成が急に進んでいます。");
        AddTransitionCandidate(rows, interval, "密度ジャンプ", neighbor.AverageEffectiveDensity - current.AverageEffectiveDensity, "実効ネットワーク密度が急増しています。");
        AddTransitionCandidate(rows, interval, "創発ジャンプ", neighbor.EmergentRate - current.EmergentRate, "創発率が急に高まっています。");
        AddTransitionCandidate(rows, interval, "安定化ジャンプ", neighbor.StableRate - current.StableRate, "安定相が強まっています。");
        AddTransitionCandidate(rows, interval, "セレンディピティジャンプ", neighbor.AverageSerendipityRate - current.AverageSerendipityRate, "セレンディピティが増えています。");
        AddTransitionCandidate(rows, interval, "知識再構成ジャンプ", neighbor.AverageKnowledgeReconfigurationScore - current.AverageKnowledgeReconfigurationScore, "知識再構成が進んでいます。");
    }

    private static void AddTransitionCandidate(
        ICollection<TransitionCandidateRow> rows,
        string interval,
        string type,
        double? delta,
        string interpretation)
    {
        if (!delta.HasValue)
        {
            return;
        }

        var value = Math.Round(delta.Value, 3);
        var threshold = type switch
        {
            "信頼ジャンプ" => 0.15,
            "密度ジャンプ" => 0.15,
            "創発ジャンプ" => 0.20,
            "安定化ジャンプ" => 0.20,
            "セレンディピティジャンプ" => 0.20,
            "知識再構成ジャンプ" => 0.15,
            _ => 0.0
        };

        if (Math.Abs(value) < threshold)
        {
            return;
        }

        rows.Add(new TransitionCandidateRow
        {
            Interval = interval,
            Type = type,
            Delta = value,
            DeltaText = FormatSigned(value),
            Interpretation = interpretation,
            ImpactLabel = GetImpactLabel(Math.Abs(value))
        });
    }

    private static List<BottleneckAnalysisRow> BuildParameterSweepBottlenecks(IReadOnlyList<ParameterSweepAnalysisPoint> points)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var total = points.Sum(item => Math.Max(item.TotalRunCount, 0));

        foreach (var point in points)
        {
            AddCount(counts, "信頼不足", point.TrustInsufficientRunCount);
            AddCount(counts, "実効密度不足", point.EffectiveDensityInsufficientRunCount);
            AddCount(counts, "StrongLink不足", point.StrongLinkInsufficientRunCount);
            AddCount(counts, "知識多様性不足", point.KnowledgeDiversityInsufficientRunCount);
            AddCount(counts, "知識再結合不足", point.KnowledgeRecombinationInsufficientRunCount);
            AddCount(counts, "知識再構成不足", point.KnowledgeReconfigurationInsufficientRunCount);
            AddCount(counts, "セレンディピティ不足", point.SerendipityInsufficientRunCount);
            AddCount(counts, "アイデア提案不足", point.IdeaProposalInsufficientRunCount);
            AddCount(counts, "情報共有不足", point.ShareInfoInsufficientRunCount);
            AddCount(counts, "建設的批判不足", point.ConstructiveCriticismInsufficientRunCount);
            AddCount(counts, "心理的安全性不足", point.PsychologicalSafetyInsufficientRunCount);

            var nonEmergentCount = Math.Max(point.TotalRunCount - point.EmergentRunCount, 0);
            if (nonEmergentCount > 0)
            {
                if (point.StableRate >= point.LearningRate)
                {
                    AddCount(counts, "安定優勢", nonEmergentCount);
                }
                else
                {
                    AddCount(counts, "学習優勢", nonEmergentCount);
                }
            }
        }

        return BuildBottleneckRows(counts, total, BottleneckInterpretation);
    }

    private static List<BottleneckAnalysisRow> BuildPhaseDiagramBottlenecks(
        IReadOnlyList<PhaseDiagramPoint> points,
        IReadOnlyDictionary<int, PhaseDiagramExperimentSummary>? experimentSummariesByExperimentId)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var total = points.Count;

        foreach (var point in points)
        {
            var experimentSummary = point.ExperimentId.HasValue
                && experimentSummariesByExperimentId is not null
                && experimentSummariesByExperimentId.TryGetValue(point.ExperimentId.Value, out var summary)
                ? summary
                : null;

            if (point.AverageTrust < 0.30)
            {
                AddCount(counts, "信頼不足", 1);
            }

            if (point.AverageEffectiveDensity < 0.30)
            {
                AddCount(counts, "実効密度不足", 1);
            }

            if ((experimentSummary?.AverageComponentCount ?? 0) >= 2 || point.DominantPhase is SimulationPhase.Silo or SimulationPhase.Chaos or SimulationPhase.Collapse)
            {
                AddCount(counts, "StrongLink不足", 1);
            }

            if (point.AverageKnowledgeDiversity < 0.50)
            {
                AddCount(counts, "知識多様性不足", 1);
            }

            if (point.AverageKnowledgeRecombinationScore < 0.25)
            {
                AddCount(counts, "知識再結合不足", 1);
            }

            if (point.AverageKnowledgeReconfigurationScore < 0.25)
            {
                AddCount(counts, "知識再構成不足", 1);
            }

            if (point.AverageSerendipityRate < 0.20)
            {
                AddCount(counts, "セレンディピティ不足", 1);
            }

            if (experimentSummary is not null && experimentSummary.AverageProposeIdeaRate < 0.07)
            {
                AddCount(counts, "アイデア提案不足", 1);
            }

            if (experimentSummary is not null && experimentSummary.AverageShareInfoRate < 0.30)
            {
                AddCount(counts, "情報共有不足", 1);
            }

            if (experimentSummary is not null && experimentSummary.AverageCriticizeSupportRatio < 0.50)
            {
                AddCount(counts, "建設的批判不足", 1);
            }

            if (point.DominantPhase == SimulationPhase.Stable || point.StableRate >= point.EmergentRate)
            {
                AddCount(counts, "安定優勢", 1);
            }
            else if (point.DominantPhase == SimulationPhase.Learning || point.LearningRate >= point.EmergentRate)
            {
                AddCount(counts, "学習優勢", 1);
            }
        }

        return BuildBottleneckRows(counts, total, BottleneckInterpretation);
    }

    private static List<BottleneckAnalysisRow> BuildBottleneckRows(
        IReadOnlyDictionary<string, int> counts,
        int total,
        Func<string, string> interpretationSelector)
    {
        return counts
            .Where(item => item.Value > 0)
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => new BottleneckAnalysisRow
            {
                Reason = item.Key,
                Count = item.Value,
                Rate = total == 0 ? 0 : Math.Round(item.Value / (double)total, 3),
                Interpretation = interpretationSelector(item.Key)
            })
            .ToList();
    }

    private static string BottleneckInterpretation(string reason) => reason switch
    {
        "信頼不足" => "信頼形成が弱く、協働が安定していません。",
        "実効密度不足" => "実効ネットワーク密度が低く、組織内の接続が十分ではありません。",
        "StrongLink不足" => "強い結びつきが少なく、ネットワークが分断されています。",
        "知識多様性不足" => "知識の多様性が低く、新しい組み合わせが生まれにくくなっています。",
        "知識再結合不足" => "知識再結合が弱く、有用な偶発的接続が生まれにくくなっています。",
        "知識再構成不足" => "知識構造の組み替えが十分に進んでいません。",
        "セレンディピティ不足" => "セレンディピティが弱く、有用な再結合のきっかけが不足しています。",
        "アイデア提案不足" => "アイデア提案が弱く、創発のきっかけが生まれにくくなっています。",
        "情報共有不足" => "情報共有が弱く、知識の流れが滞っています。",
        "建設的批判不足" => "建設的批判が弱く、再配線のきっかけが不足しています。",
        "心理的安全性不足" => "心理的安全性が低く、批判や提案が知識再結合につながりにくくなっています。",
        "安定優勢" => "ネットワークは安定していますが、探索や再結合はまだ弱い状態です。",
        "学習優勢" => "学習は進んでいますが、相変化にはまだ到達していません。",
        _ => "この要因が創発のボトルネックになっている可能性があります。"
    };

    private static string BuildParameterSweepComment(
        double maxEmergentRate,
        double maxEmergentScore,
        double maxAverageTrust,
        double maxEffectiveDensity,
        double maxSerendipityRate,
        double maxKnowledgeReconfiguration,
        string recommendedRegion)
    {
        if (maxEmergentRate > 0)
        {
            return $"一部の条件で創発が観測されています。{recommendedRegion} の周辺を重点的に確認してください。";
        }

        if (maxAverageTrust < 0.3)
        {
            return "信頼ネットワーク形成が弱すぎます。信頼成長率、閾値、減衰率を見直してください。";
        }

        if (maxEffectiveDensity < 0.3)
        {
            return "実効ネットワーク密度が低すぎます。閾値や信頼形成条件が厳しすぎる可能性があります。";
        }

        if (maxSerendipityRate == 0)
        {
            return "セレンディピティが観測されませんでした。探索傾向、セレンディピティ感度、閾値を見直してください。";
        }

        if (maxKnowledgeReconfiguration < 0.4)
        {
            return "知識再構成が弱すぎます。異分野接触、再配線感度、再結合率を見直してください。";
        }

        if (maxEmergentScore > 0)
        {
            return "創発スコアは上がっていますが、相判定条件のどこかがボトルネックになっている可能性があります。";
        }

        return "信頼・知識・セレンディピティは見えていますが、まだ創発へは到達していません。";
    }

    private static string BuildParameterSweepConclusion(
        IReadOnlyList<ParameterSweepAnalysisPoint> points,
        double maxEmergentRate,
        double maxEmergentScore,
        double maxEffectiveDensity)
    {
        if (maxEmergentRate > 0)
        {
            return "このスイープは創発に直接影響しています。";
        }

        if (maxEmergentScore > 0 && maxEffectiveDensity >= 0.7)
        {
            return "プロセスは創発に近づいていますが、相変化はまだ起きていません。";
        }

        if (maxEffectiveDensity < 0.3)
        {
            return "システムはまだ信頼ネットワーク形成の初期段階です。";
        }

        return points.Count == 0 ? "--" : "このスイープでは直接的な創発は観測されませんでした。";
    }

    private static string BuildRecommendedParameterRegion(
        IReadOnlyList<ParameterSweepAnalysisPoint> ordered,
        double maxEmergentRate,
        double maxEmergentScore,
        double maxEffectiveDensity,
        double maxKnowledgeReconfiguration)
    {
        if (ordered.Count == 0)
        {
            return "--";
        }

        IEnumerable<ParameterSweepAnalysisPoint> candidatePoints;
        string reason;

        if (maxEmergentRate > 0)
        {
            candidatePoints = ordered.Where(item => item.EmergentRate >= maxEmergentRate * 0.8);
            reason = "創発率が最大値の80%以上";
        }
        else if (maxEmergentScore > 0)
        {
            candidatePoints = ordered.Where(item => item.AverageEmergentScore >= maxEmergentScore * 0.8);
            reason = "平均創発スコアが高い";
        }
        else
        {
            candidatePoints = ordered.Where(item =>
                item.EffectiveDensity >= maxEffectiveDensity * 0.85 &&
                item.AverageKnowledgeReconfigurationScore >= maxKnowledgeReconfiguration * 0.85);
            reason = "実効密度と知識再構成スコアが両方高い";
        }

        var selected = candidatePoints.ToList();
        if (selected.Count == 0)
        {
            selected = ordered
                .OrderByDescending(item => item.EffectiveDensity + item.AverageKnowledgeReconfigurationScore + item.AverageEmergentScore)
                .Take(Math.Max(1, ordered.Count / 3))
                .ToList();
            reason = "合成スコアの高い領域";
        }

        return $"{DescribeRange(selected.Select(item => item.ParameterValue))} / {reason}";
    }

    private static string BuildPhaseDiagramComment(
        double maxEmergentRate,
        double maxPipelineCompletion,
        double maxAverageTrust,
        double maxEffectiveDensity,
        double maxSerendipityRate,
        double maxKnowledgeReconfiguration,
        string dangerRegion)
    {
        if (maxEmergentRate > 0)
        {
            return $"一部のセルで創発が観測されています。{dangerRegion} の周辺を探索してください。";
        }

        if (maxPipelineCompletion >= 0.7)
        {
            return "プロセスは進んでいますが、まだ相変化には到達していません。";
        }

        if (maxAverageTrust < 0.3)
        {
            return "平均信頼度が低く、ネットワークが安定していません。";
        }

        if (maxEffectiveDensity < 0.3)
        {
            return "実効密度が低く、ネットワークが十分につながっていません。";
        }

        if (maxSerendipityRate == 0)
        {
            return "セレンディピティが観測されませんでした。探索設定を見直してください。";
        }

        if (maxKnowledgeReconfiguration < 0.4)
        {
            return "知識再構成が弱く、創発が進みにくくなっています。";
        }

        return $"信頼と密度は高いものの、創発にはまだ伸びしろがあります。危険領域: {dangerRegion}";
    }

    private static string BuildRecommendedPhaseRegion(
        IReadOnlyList<PhaseDiagramPoint> ordered,
        double maxEmergentRate,
        double maxPipelineCompletion,
        double maxCompositeScore)
    {
        if (ordered.Count == 0)
        {
            return "--";
        }

        IEnumerable<PhaseDiagramPoint> candidatePoints;
        string reason;

        if (maxEmergentRate > 0)
        {
            candidatePoints = ordered.Where(item => item.EmergentRate >= maxEmergentRate * 0.8);
            reason = "創発率が最大値の80%以上";
        }
        else if (maxPipelineCompletion > 0)
        {
            candidatePoints = ordered.Where(item => item.AveragePipelineCompletionScore >= maxPipelineCompletion * 0.8);
            reason = "パイプライン完了度が高い";
        }
        else if (maxCompositeScore > 0)
        {
            candidatePoints = ordered.Where(item =>
                (item.AverageEffectiveDensity + item.AverageKnowledgeReconfigurationScore + item.AverageKnowledgeRecombinationScore) / 3.0 >= maxCompositeScore * 0.8);
            reason = "実効密度と知識指標が高い";
        }
        else
        {
            candidatePoints = ordered.Where(item =>
                item.AverageEffectiveDensity >= 0.30 &&
                item.AverageKnowledgeReconfigurationScore >= 0.30);
            reason = "実効密度と知識再構成スコアが両方高い";
        }

        var selected = candidatePoints.ToList();
        if (selected.Count == 0)
        {
            selected = ordered
                .OrderByDescending(item => item.EmergentRate + item.AveragePipelineCompletionScore + item.AverageEffectiveDensity)
                .Take(Math.Max(1, ordered.Count / 3))
                .ToList();
            reason = "合成スコアの高いセル領域";
        }

        return $"{DescribePhaseRegion(selected)} / {reason}";
    }

    private static string BuildDangerRegion(
        IReadOnlyList<PhaseDiagramPoint> ordered,
        IReadOnlyDictionary<int, PhaseDiagramExperimentSummary>? experimentSummariesByExperimentId)
    {
        var selected = ordered.Where(item =>
        {
            var experimentSummary = item.ExperimentId.HasValue
                && experimentSummariesByExperimentId is not null
                && experimentSummariesByExperimentId.TryGetValue(item.ExperimentId.Value, out var summary)
                ? summary
                : null;

            return item.SiloRate >= 0.35
                   || item.CollapseRate >= 0.20
                   || item.ChaosRate >= 0.20
                   || item.AverageTrust < 0.30
                   || item.AverageEffectiveDensity < 0.30
                   || (experimentSummary?.AverageComponentCount ?? 0) >= 2;
        }).ToList();

        if (selected.Count == 0)
        {
            return "危険領域は見つかりませんでした。";
        }

        return $"{DescribePhaseRegion(selected)} / サイロ、混沌、崩壊、低信頼、低密度";
    }

    private static string DescribeRange(IEnumerable<double> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? "--" : $"{list.Min():0.000} - {list.Max():0.000}";
    }

    private static string DescribePhaseRegion(IReadOnlyCollection<PhaseDiagramPoint> selected)
    {
        if (selected.Count == 0)
        {
            return "--";
        }

        var xValues = selected.Select(item => item.XValue).ToList();
        var yValues = selected.Select(item => item.YValue).ToList();
        return $"X {xValues.Min():0.000} - {xValues.Max():0.000} / Y {yValues.Min():0.000} - {yValues.Max():0.000} ({selected.Count} points)";
    }

    private static string GetGridKey(double xValue, double yValue)
    {
        return $"{Math.Round(xValue, 3).ToString("0.000", CultureInfo.InvariantCulture)}|{Math.Round(yValue, 3).ToString("0.000", CultureInfo.InvariantCulture)}";
    }

    private static string FormatPointValue<T>(T? point, Func<T, double> selector) where T : class
    {
        if (point is null)
        {
            return "--";
        }

        var parameterValue = point switch
        {
            ParameterSweepAnalysisPoint sweepPoint => sweepPoint.ParameterValue,
            PhaseDiagramPoint phasePoint => phasePoint.XValue,
            _ => 0
        };

        return $"{parameterValue:0.000} ({selector(point):0.000})";
    }

    private static string FormatPointCell<T>(T? point, Func<T, double> selector) where T : class
    {
        if (point is null)
        {
            return "--";
        }

        return point switch
        {
            PhaseDiagramPoint phasePoint => $"X={phasePoint.XValue:0.000}, Y={phasePoint.YValue:0.000} ({selector(point):0.000})",
            _ => FormatPointValue(point, selector)
        };
    }

    private static T? SelectBest<T>(IReadOnlyList<T> points, Func<T, double> selector) where T : class
    {
        return points
            .OrderByDescending(selector)
            .ThenBy(item => item switch
            {
                ParameterSweepAnalysisPoint sweepPoint => sweepPoint.ParameterValue,
                PhaseDiagramPoint phasePoint => phasePoint.XValue + phasePoint.YValue,
                _ => 0
            })
            .FirstOrDefault();
    }

    private static ParameterSweepAnalysisPoint? SelectBestDelta(
        IReadOnlyList<ParameterSweepAnalysisPoint> points,
        Func<ParameterSweepAnalysisPoint, double?> selector)
    {
        return points
            .Where(item => selector(item).HasValue)
            .OrderByDescending(item => selector(item)!.Value)
            .ThenBy(item => item.ParameterValue)
            .FirstOrDefault();
    }

    private static string FormatInterval(double? previous, double? current, double? delta)
    {
        if (!previous.HasValue || !current.HasValue || !delta.HasValue)
        {
            return "--";
        }

        return $"{previous.Value:0.000} -> {current.Value:0.000} ({FormatSigned(delta.Value)})";
    }

    private static string FormatSigned(double value)
    {
        return value >= 0
            ? $"+{value:0.000}"
            : value.ToString("0.000", CultureInfo.InvariantCulture);
    }

    private static void AddCount(IDictionary<string, int> counts, string reason, int value)
    {
        if (value <= 0)
        {
            return;
        }

        counts[reason] = counts.TryGetValue(reason, out var existing) ? existing + value : value;
    }

    private static string GetImpactLabel(double magnitude)
    {
        if (magnitude >= 0.20)
        {
            return "strong";
        }

        if (magnitude >= 0.05)
        {
            return "medium";
        }

        return "weak";
    }

    private static string BuildCsv(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        value ??= "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

public sealed class ParameterSweepInterpretationResult
{
    public static ParameterSweepInterpretationResult Empty { get; } = new()
    {
        SummaryCards =
        [
            new() { Label = "最大創発率のParameterValue", Value = "--" },
            new() { Label = "最大安定率のParameterValue", Value = "--" },
            new() { Label = "最大学習率のParameterValue", Value = "--" },
            new() { Label = "最大平均信頼度のParameterValue", Value = "--" },
            new() { Label = "最大実効密度のParameterValue", Value = "--" },
            new() { Label = "最大セレンディピティ率のParameterValue", Value = "--" },
            new() { Label = "最大知識再構成スコアのParameterValue", Value = "--" },
            new() { Label = "最大知的敬意のParameterValue", Value = "--" },
            new() { Label = "最大アイデア受容スコアのParameterValue", Value = "--" },
            new() { Label = "最大相互師匠スコアのParameterValue", Value = "--" },
            new() { Label = "最大知的敬意再構成のParameterValue", Value = "--" },
            new() { Label = "最大知的敬意セレンディピティのParameterValue", Value = "--" },
            new() { Label = "最大知的敬意創発のParameterValue", Value = "--" },
            new() { Label = "最大信頼ジャンプ区間", Value = "--" },
            new() { Label = "最大創発ジャンプ区間", Value = "--" },
            new() { Label = "推奨パラメータ領域", Value = "--" }
        ]
    };

    public List<ParameterSweepSummaryCard> SummaryCards { get; init; } = [];
    public string RecommendedRegion { get; init; } = "--";
    public string AutoComment { get; init; } = "--";
    public string PrimaryConclusion { get; init; } = "--";
    public List<TransitionCandidateRow> TransitionCandidates { get; init; } = [];
    public List<BottleneckAnalysisRow> BottleneckRows { get; init; } = [];
}

public sealed class PhaseDiagramInterpretationResult
{
    public static PhaseDiagramInterpretationResult Empty { get; } = new()
    {
        SummaryCards =
        [
            new() { Label = "最大創発セル", Value = "--" },
            new() { Label = "最大安定セル", Value = "--" },
            new() { Label = "最大学習セル", Value = "--" },
            new() { Label = "最大平均信頼セル", Value = "--" },
            new() { Label = "最大実効密度セル", Value = "--" },
            new() { Label = "最大セレンディピティセル", Value = "--" },
            new() { Label = "最大知識再構成セル", Value = "--" },
            new() { Label = "平均知的敬意", Value = "--" },
            new() { Label = "平均知的敬意密度", Value = "--" },
            new() { Label = "平均知的敬意多様性", Value = "--" },
            new() { Label = "平均アイデア受容", Value = "--" },
            new() { Label = "平均相互師匠スコア", Value = "--" },
            new() { Label = "平均知的敬意再構成", Value = "--" },
            new() { Label = "平均知的敬意セレンディピティ", Value = "--" },
            new() { Label = "平均知的敬意創発", Value = "--" },
            new() { Label = "推奨領域", Value = "--" },
            new() { Label = "危険領域", Value = "--" }
        ]
    };

    public List<PhaseDiagramSummaryCard> SummaryCards { get; init; } = [];
    public string RecommendedRegion { get; init; } = "--";
    public string DangerRegion { get; init; } = "--";
    public string AutoComment { get; init; } = "--";
    public List<TransitionCandidateRow> TransitionCandidates { get; init; } = [];
    public List<BottleneckAnalysisRow> BottleneckRows { get; init; } = [];
}

public sealed class TransitionCandidateRow
{
    public string Interval { get; init; } = "--";
    public string Type { get; init; } = "--";
    public double Delta { get; init; }
    public string DeltaText { get; init; } = "--";
    public string ImpactLabel { get; init; } = "weak";
    public string Interpretation { get; init; } = "--";
}

public sealed class BottleneckAnalysisRow
{
    public string Reason { get; init; } = "--";
    public int Count { get; init; }
    public double Rate { get; init; }
    public string Interpretation { get; init; } = "--";
}

public sealed class PhaseDiagramExperimentSummary
{
    public double AverageComponentCount { get; init; }
    public double AverageShareInfoRate { get; init; }
    public double AverageProposeIdeaRate { get; init; }
    public double AverageCriticizeSupportRatio { get; init; }
    public bool ThinkingSpeedModelEnabled { get; init; }
    public double AverageThinkingSpeed { get; init; }
    public double MinThinkingSpeed { get; init; }
    public double MaxThinkingSpeed { get; init; }
    public double ThinkingSpeedDispersionActual { get; init; }
    public double OrganizationalDecisionSpeed { get; init; }
    public double OrganizationalValidationSpeed { get; init; }
    public double GeneratedIdeaCount { get; init; }
    public double ProcessedIdeaCount { get; init; }
    public double UnprocessedIdeaCount { get; init; }
    public double GeneratedHypothesisCount { get; init; }
    public double ValidatedHypothesisCount { get; init; }
    public double UnvalidatedHypothesisCount { get; init; }
    public double CognitiveLoad { get; init; }
    public double ThinkingSpeedMismatch { get; init; }
    public double DecisionOverloadRatio { get; init; }
    public double ValidationOverloadRatio { get; init; }
    public int ConsecutiveHighLoadSteps { get; init; }
    public double ThinkingFlowBonus { get; init; }
    public double ThinkingOverloadPenalty { get; init; }
    public double AverageRespect { get; init; }
    public double AverageIntellectualRespect { get; init; }
    public double IntellectualRespectDensity { get; init; }
    public double IntellectualRespectDiversityIndex { get; init; }
    public double IntellectualRespectConcentration { get; init; }
    public double IdeaAcceptanceScore { get; init; }
    public double AverageChallengeAcceptanceScore { get; init; }
    public double AverageRespectReconfigurationBoost { get; init; }
    public double AverageRespectEmergenceComponent { get; init; }
    public double AverageIntellectualRespectReconfigurationComponent { get; init; }
    public double AverageIntellectualRespectSerendipityComponent { get; init; }
    public double AverageIntellectualRespectEmergenceComponent { get; init; }
    public double AverageEgoPenaltyApplied { get; init; }
    public double AverageHierarchyPenaltyApplied { get; init; }
    public double MutualMentorshipScore { get; init; }
    public int MentorshipLinkCount { get; init; }
    public int CrossMentorshipLinkCount { get; init; }
    public double MentorshipDiversityIndex { get; init; }
    public double LearningFromOthersScore { get; init; }
    public int LearnedFromUnexpectedAgentCount { get; init; }
    public double AverageThanksCoinToReconfigurationContribution { get; init; }
    public double AverageThanksCoinToSerendipityContribution { get; init; }
    public double AverageThanksCoinToEmergenceContribution { get; init; }
    public double PopularityTrapRate { get; init; }
}
