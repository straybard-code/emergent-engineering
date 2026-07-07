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
                new() { Label = "Max EmergentRate ParameterValue", Value = FormatPointValue(bestEmergent, item => item.EmergentRate) },
                new() { Label = "Max StableRate ParameterValue", Value = FormatPointValue(bestStable, item => item.StableRate) },
                new() { Label = "Max LearningRate ParameterValue", Value = FormatPointValue(bestLearning, item => item.LearningRate) },
                new() { Label = "Max AverageTrust ParameterValue", Value = FormatPointValue(bestTrust, item => item.AverageTrust) },
                new() { Label = "Max EffectiveDensity ParameterValue", Value = FormatPointValue(bestDensity, item => item.EffectiveDensity) },
                new() { Label = "Max SerendipityRate ParameterValue", Value = FormatPointValue(bestSerendipity, item => item.SerendipityRate) },
                new() { Label = "Max KnowledgeReconfigurationScore ParameterValue", Value = FormatPointValue(bestReconfiguration, item => item.AverageKnowledgeReconfigurationScore) },
                new() { Label = "Max AverageIntellectualRespect ParameterValue", Value = FormatPointValue(bestIntellectualRespect, item => item.AverageIntellectualRespect) },
                new() { Label = "Max IdeaAcceptanceScore ParameterValue", Value = FormatPointValue(bestIdeaAcceptance, item => item.IdeaAcceptanceScore) },
                new() { Label = "Max MutualMentorshipScore ParameterValue", Value = FormatPointValue(bestMutualMentorship, item => item.MutualMentorshipScore) },
                new() { Label = "Max IntellectualRespectReconfiguration ParameterValue", Value = FormatPointValue(bestIntellectualReconfiguration, item => item.AverageIntellectualRespectReconfigurationComponent) },
                new() { Label = "Max IntellectualRespectSerendipity ParameterValue", Value = FormatPointValue(bestIntellectualSerendipity, item => item.AverageIntellectualRespectSerendipityComponent) },
                new() { Label = "Max IntellectualRespectEmergence ParameterValue", Value = FormatPointValue(bestIntellectualEmergence, item => item.AverageIntellectualRespectEmergenceComponent) },
                new() { Label = "Max Trust Jump Interval", Value = FormatInterval(bestTrustJump?.PreviousParameterValue, bestTrustJump?.ParameterValue, bestTrustJump?.DeltaAverageTrust) },
                new() { Label = "Max Emergent Jump Interval", Value = FormatInterval(bestEmergentJump?.PreviousParameterValue, bestEmergentJump?.ParameterValue, bestEmergentJump?.DeltaEmergentRate) },
                new() { Label = "Recommended Parameter Region", Value = recommendedRegion }
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
                new() { Label = "Max EmergentRate Cell", Value = FormatPointCell(maxEmergent, item => item.EmergentRate) },
                new() { Label = "Max StableRate Cell", Value = FormatPointCell(maxStable, item => item.StableRate) },
                new() { Label = "Max LearningRate Cell", Value = FormatPointCell(maxLearning, item => item.LearningRate) },
                new() { Label = "Max AverageTrust Cell", Value = FormatPointCell(maxTrust, item => item.AverageTrust) },
                new() { Label = "Max EffectiveDensity Cell", Value = FormatPointCell(maxDensity, item => item.AverageEffectiveDensity) },
                new() { Label = "Max SerendipityRate Cell", Value = FormatPointCell(maxSerendipity, item => item.AverageSerendipityRate) },
                new() { Label = "Max KnowledgeReconfigurationScore Cell", Value = FormatPointCell(maxReconfiguration, item => item.AverageKnowledgeReconfigurationScore) },
                new() { Label = "AverageIntellectualRespect", Value = averageIntellectualRespect.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "AverageIntellectualRespectDensity", Value = averageIntellectualRespectDensity.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "AverageIntellectualRespectDiversityIndex", Value = averageIntellectualRespectDiversityIndex.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "AverageIdeaAcceptanceScore", Value = averageIdeaAcceptanceScore.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "AverageMutualMentorshipScore", Value = averageMutualMentorshipScore.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "AverageIntellectualRespectReconfiguration", Value = averageIntellectualRespectReconfiguration.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "AverageIntellectualRespectSerendipity", Value = averageIntellectualRespectSerendipity.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "AverageIntellectualRespectEmergence", Value = averageIntellectualRespectEmergence.ToString("0.000", CultureInfo.InvariantCulture) },
                new() { Label = "Recommended Region", Value = recommendedRegion },
                new() { Label = "Danger Region", Value = dangerRegion }
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

            AddTransitionCandidate(rows, interval, "Trust Jump", current.DeltaAverageTrust, "Trust network formation has accelerated.");
            AddTransitionCandidate(rows, interval, "Density Jump", current.DeltaEffectiveDensity, "Effective network density increased quickly.");
            AddTransitionCandidate(rows, interval, "Emergence Jump", current.DeltaEmergentRate, "Emergent phase rate increased quickly.");
            AddTransitionCandidate(rows, interval, "Stabilization Jump", Math.Round(current.StableRate - previous.StableRate, 3), "Stable phase increased.");
            AddTransitionCandidate(rows, interval, "Serendipity Jump", current.DeltaSerendipityRate, "Serendipity increased.");
            AddTransitionCandidate(rows, interval, "Reconfiguration Jump", current.DeltaKnowledgeReconfigurationScore, "Knowledge reconfiguration increased.");
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

        AddTransitionCandidate(rows, interval, "Trust Jump", neighbor.AverageTrust - current.AverageTrust, "Trust network strength changed.");
        AddTransitionCandidate(rows, interval, "Density Jump", neighbor.AverageEffectiveDensity - current.AverageEffectiveDensity, "Effective density changed.");
        AddTransitionCandidate(rows, interval, "Emergence Jump", neighbor.EmergentRate - current.EmergentRate, "Emergent rate changed.");
        AddTransitionCandidate(rows, interval, "Stabilization Jump", neighbor.StableRate - current.StableRate, "Stable phase changed.");
        AddTransitionCandidate(rows, interval, "Serendipity Jump", neighbor.AverageSerendipityRate - current.AverageSerendipityRate, "Serendipity changed.");
        AddTransitionCandidate(rows, interval, "Reconfiguration Jump", neighbor.AverageKnowledgeReconfigurationScore - current.AverageKnowledgeReconfigurationScore, "Knowledge reconfiguration changed.");
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
            "Trust Jump" => 0.15,
            "Density Jump" => 0.15,
            "Emergence Jump" => 0.20,
            "Stabilization Jump" => 0.20,
            "Serendipity Jump" => 0.20,
            "Reconfiguration Jump" => 0.15,
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
            AddCount(counts, "Trust Insufficient", point.TrustInsufficientRunCount);
            AddCount(counts, "Effective Density Insufficient", point.EffectiveDensityInsufficientRunCount);
            AddCount(counts, "StrongLink Insufficient", point.StrongLinkInsufficientRunCount);
            AddCount(counts, "Knowledge Diversity Insufficient", point.KnowledgeDiversityInsufficientRunCount);
            AddCount(counts, "Knowledge Recombination Insufficient", point.KnowledgeRecombinationInsufficientRunCount);
            AddCount(counts, "Knowledge Reconfiguration Insufficient", point.KnowledgeReconfigurationInsufficientRunCount);
            AddCount(counts, "Serendipity Insufficient", point.SerendipityInsufficientRunCount);
            AddCount(counts, "Idea Proposal Insufficient", point.IdeaProposalInsufficientRunCount);
            AddCount(counts, "Share Info Insufficient", point.ShareInfoInsufficientRunCount);
            AddCount(counts, "Constructive Criticism Insufficient", point.ConstructiveCriticismInsufficientRunCount);
            AddCount(counts, "Psychological Safety Insufficient", point.PsychologicalSafetyInsufficientRunCount);

            var nonEmergentCount = Math.Max(point.TotalRunCount - point.EmergentRunCount, 0);
            if (nonEmergentCount > 0)
            {
                if (point.StableRate >= point.LearningRate)
                {
                    AddCount(counts, "Stable Dominant", nonEmergentCount);
                }
                else
                {
                    AddCount(counts, "Learning Dominant", nonEmergentCount);
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
                AddCount(counts, "Trust Insufficient", 1);
            }

            if (point.AverageEffectiveDensity < 0.30)
            {
                AddCount(counts, "Effective Density Insufficient", 1);
            }

            if ((experimentSummary?.AverageComponentCount ?? 0) >= 2 || point.DominantPhase is SimulationPhase.Silo or SimulationPhase.Chaos or SimulationPhase.Collapse)
            {
                AddCount(counts, "StrongLink Insufficient", 1);
            }

            if (point.AverageKnowledgeDiversity < 0.50)
            {
                AddCount(counts, "Knowledge Diversity Insufficient", 1);
            }

            if (point.AverageKnowledgeRecombinationScore < 0.25)
            {
                AddCount(counts, "Knowledge Recombination Insufficient", 1);
            }

            if (point.AverageKnowledgeReconfigurationScore < 0.25)
            {
                AddCount(counts, "Knowledge Reconfiguration Insufficient", 1);
            }

            if (point.AverageSerendipityRate < 0.20)
            {
                AddCount(counts, "Serendipity Insufficient", 1);
            }

            if (experimentSummary is not null && experimentSummary.AverageProposeIdeaRate < 0.07)
            {
                AddCount(counts, "Idea Proposal Insufficient", 1);
            }

            if (experimentSummary is not null && experimentSummary.AverageShareInfoRate < 0.30)
            {
                AddCount(counts, "Share Info Insufficient", 1);
            }

            if (experimentSummary is not null && experimentSummary.AverageCriticizeSupportRatio < 0.50)
            {
                AddCount(counts, "Constructive Criticism Insufficient", 1);
            }

            if (point.DominantPhase == SimulationPhase.Stable || point.StableRate >= point.EmergentRate)
            {
                AddCount(counts, "Stable Dominant", 1);
            }
            else if (point.DominantPhase == SimulationPhase.Learning || point.LearningRate >= point.EmergentRate)
            {
                AddCount(counts, "Learning Dominant", 1);
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
        "Trust Insufficient" => "Trust formation is too weak for collaboration to stabilize.",
        "Effective Density Insufficient" => "Effective network density is too low for the organization to connect.",
        "StrongLink Insufficient" => "Strong links are too sparse and the network remains fragmented.",
        "Knowledge Diversity Insufficient" => "Knowledge diversity is too low for new combinations to emerge.",
        "Knowledge Recombination Insufficient" => "Knowledge recombination is too weak to generate useful accidental links.",
        "Knowledge Reconfiguration Insufficient" => "Knowledge structure is not being reconfigured enough.",
        "Serendipity Insufficient" => "Serendipity is too weak to seed useful recombination.",
        "Idea Proposal Insufficient" => "Idea proposal is too weak to seed emergence.",
        "Share Info Insufficient" => "Information sharing is too weak for knowledge to flow.",
        "Constructive Criticism Insufficient" => "Constructive criticism is too weak to stimulate rewiring.",
        "Psychological Safety Insufficient" => "Psychological safety is too weak for criticism and proposals to help recombination.",
        "Stable Dominant" => "The network is stable, but exploration and recombination remain weak.",
        "Learning Dominant" => "Learning is progressing, but the system has not crossed into phase change.",
        _ => "This factor may be a bottleneck to emergence."
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
            return $"Emergence was observed in some conditions. Focus on the region around: {recommendedRegion}";
        }

        if (maxAverageTrust < 0.3)
        {
            return "Trust network formation is too weak. Check trust growth rate, effective trust threshold, and decay.";
        }

        if (maxEffectiveDensity < 0.3)
        {
            return "Effective network density is too low. Thresholds or trust formation may be too strict.";
        }

        if (maxSerendipityRate == 0)
        {
            return "Serendipity did not occur. Review exploration tendency, serendipity sensitivity, and threshold.";
        }

        if (maxKnowledgeReconfiguration < 0.4)
        {
            return "Knowledge reconfiguration is too weak. Review cross-domain exposure, rewiring sensitivity, and recombination rate.";
        }

        if (maxEmergentScore > 0)
        {
            return "Emergence score is rising, but phase gating or another condition may be the bottleneck.";
        }

        return "Trust, knowledge, and serendipity are present, but the system has not yet crossed into emergence.";
    }

    private static string BuildParameterSweepConclusion(
        IReadOnlyList<ParameterSweepAnalysisPoint> points,
        double maxEmergentRate,
        double maxEmergentScore,
        double maxEffectiveDensity)
    {
        if (maxEmergentRate > 0)
        {
            return "This sweep affects emergence directly.";
        }

        if (maxEmergentScore > 0 && maxEffectiveDensity >= 0.7)
        {
            return "The process approaches emergence, but phase change still does not occur.";
        }

        if (maxEffectiveDensity < 0.3)
        {
            return "The system is still early in trust network formation.";
        }

        return points.Count == 0 ? "--" : "No direct emergence was observed in this sweep.";
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
            reason = "EmergentRate >= 80% of maximum";
        }
        else if (maxEmergentScore > 0)
        {
            candidatePoints = ordered.Where(item => item.AverageEmergentScore >= maxEmergentScore * 0.8);
            reason = "AverageEmergentScore is high";
        }
        else
        {
            candidatePoints = ordered.Where(item =>
                item.EffectiveDensity >= maxEffectiveDensity * 0.85 &&
                item.AverageKnowledgeReconfigurationScore >= maxKnowledgeReconfiguration * 0.85);
            reason = "EffectiveDensity and KnowledgeReconfigurationScore are both high";
        }

        var selected = candidatePoints.ToList();
        if (selected.Count == 0)
        {
            selected = ordered
                .OrderByDescending(item => item.EffectiveDensity + item.AverageKnowledgeReconfigurationScore + item.AverageEmergentScore)
                .Take(Math.Max(1, ordered.Count / 3))
                .ToList();
            reason = "Top combined score region";
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
            return $"Emergence was observed in some cells. Explore around: {dangerRegion}";
        }

        if (maxPipelineCompletion >= 0.7)
        {
            return "The process is progressing, but phase change has not yet occurred.";
        }

        if (maxAverageTrust < 0.3)
        {
            return "Average trust is too low for the network to stabilize.";
        }

        if (maxEffectiveDensity < 0.3)
        {
            return "Effective density is too low for the network to connect.";
        }

        if (maxSerendipityRate == 0)
        {
            return "Serendipity has not been observed. Review exploration settings.";
        }

        if (maxKnowledgeReconfiguration < 0.4)
        {
            return "Knowledge reconfiguration is too weak for emergence to proceed.";
        }

        return $"High trust and density exist, but emergence still has room to develop. Danger region: {dangerRegion}";
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
            reason = "EmergentRate >= 80% of maximum";
        }
        else if (maxPipelineCompletion > 0)
        {
            candidatePoints = ordered.Where(item => item.AveragePipelineCompletionScore >= maxPipelineCompletion * 0.8);
            reason = "Pipeline completion is high";
        }
        else if (maxCompositeScore > 0)
        {
            candidatePoints = ordered.Where(item =>
                (item.AverageEffectiveDensity + item.AverageKnowledgeReconfigurationScore + item.AverageKnowledgeRecombinationScore) / 3.0 >= maxCompositeScore * 0.8);
            reason = "EffectiveDensity and knowledge metrics are high";
        }
        else
        {
            candidatePoints = ordered.Where(item =>
                item.AverageEffectiveDensity >= 0.30 &&
                item.AverageKnowledgeReconfigurationScore >= 0.30);
            reason = "EffectiveDensity and KnowledgeReconfigurationScore are both high";
        }

        var selected = candidatePoints.ToList();
        if (selected.Count == 0)
        {
            selected = ordered
                .OrderByDescending(item => item.EmergentRate + item.AveragePipelineCompletionScore + item.AverageEffectiveDensity)
                .Take(Math.Max(1, ordered.Count / 3))
                .ToList();
            reason = "Top combined score cell region";
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
            return "No danger region detected.";
        }

        return $"{DescribePhaseRegion(selected)} / Silo, Chaos, Collapse, low trust, or low density";
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
            new() { Label = "Max EmergentRate ParameterValue", Value = "--" },
            new() { Label = "Max StableRate ParameterValue", Value = "--" },
            new() { Label = "Max LearningRate ParameterValue", Value = "--" },
            new() { Label = "Max AverageTrust ParameterValue", Value = "--" },
            new() { Label = "Max EffectiveDensity ParameterValue", Value = "--" },
            new() { Label = "Max SerendipityRate ParameterValue", Value = "--" },
            new() { Label = "Max KnowledgeReconfigurationScore ParameterValue", Value = "--" },
            new() { Label = "Max AverageIntellectualRespect ParameterValue", Value = "--" },
            new() { Label = "Max IdeaAcceptanceScore ParameterValue", Value = "--" },
            new() { Label = "Max MutualMentorshipScore ParameterValue", Value = "--" },
            new() { Label = "Max IntellectualRespectReconfiguration ParameterValue", Value = "--" },
            new() { Label = "Max IntellectualRespectSerendipity ParameterValue", Value = "--" },
            new() { Label = "Max IntellectualRespectEmergence ParameterValue", Value = "--" },
            new() { Label = "Max Trust Jump Interval", Value = "--" },
            new() { Label = "Max Emergent Jump Interval", Value = "--" },
            new() { Label = "Recommended Parameter Region", Value = "--" }
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
            new() { Label = "Max EmergentRate Cell", Value = "--" },
            new() { Label = "Max StableRate Cell", Value = "--" },
            new() { Label = "Max LearningRate Cell", Value = "--" },
            new() { Label = "Max AverageTrust Cell", Value = "--" },
            new() { Label = "Max EffectiveDensity Cell", Value = "--" },
            new() { Label = "Max SerendipityRate Cell", Value = "--" },
            new() { Label = "Max KnowledgeReconfigurationScore Cell", Value = "--" },
            new() { Label = "AverageIntellectualRespect", Value = "--" },
            new() { Label = "AverageIntellectualRespectDensity", Value = "--" },
            new() { Label = "AverageIntellectualRespectDiversityIndex", Value = "--" },
            new() { Label = "AverageIdeaAcceptanceScore", Value = "--" },
            new() { Label = "AverageMutualMentorshipScore", Value = "--" },
            new() { Label = "AverageIntellectualRespectReconfiguration", Value = "--" },
            new() { Label = "AverageIntellectualRespectSerendipity", Value = "--" },
            new() { Label = "AverageIntellectualRespectEmergence", Value = "--" },
            new() { Label = "Recommended Region", Value = "--" },
            new() { Label = "Danger Region", Value = "--" }
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
