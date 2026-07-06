using System.Globalization;
using System.Text;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class DetailsModel(
    AppDbContext db,
    PhaseDiagramRunner phaseDiagramRunner,
    ExperimentInterpretationService interpretationService) : PageModel
{
    [TempData]
    public string? DiagramMessage { get; set; }

    public PhaseDiagram? Diagram { get; private set; }
    public string ScenarioName { get; private set; } = "-";
    public List<PhaseDiagramPoint> Points { get; private set; } = [];
    public List<double> XValues { get; private set; } = [];
    public List<double> YValues { get; private set; } = [];
    public List<PhaseDiagramSummaryCard> SummaryCards { get; private set; } = [];
    public string RecommendedInterpretation { get; private set; } = "--";
    public string RecommendedReseekArea { get; private set; } = "--";
    public string AnalysisComment { get; private set; } = "--";
    public string RecommendedRegionText { get; private set; } = "--";
    public string DangerRegionText { get; private set; } = "--";
    public List<TransitionCandidateRow> TransitionCandidateRows { get; private set; } = [];
    public List<BottleneckAnalysisRow> BottleneckAnalysisRows { get; private set; } = [];
    public List<PhaseDiagramHeatmapRow> DominantPhaseRows { get; private set; } = [];
    public List<PhaseDiagramHeatmapRow> EmergentRateRows { get; private set; } = [];
    public List<PhaseDiagramHeatmapRow> PipelineCompletionRows { get; private set; } = [];
    public List<PhaseDiagramHeatmapRow> BottleneckRows { get; private set; } = [];
    public List<PhaseDiagramPointResultRow> PointResults { get; private set; } = [];
    public List<PhaseDiagramBoundaryEntry> BoundaryEntries { get; private set; } = [];
    public int BoundaryCandidateCount { get; private set; }
    public int EmergentBoundaryCount { get; private set; }
    public int StableBoundaryCount { get; private set; }
    public int SiloBoundaryCount { get; private set; }
    public string MostCommonBoundaryPattern { get; private set; } = "--";
    public PhaseDiagramRegionSummary EmergentRegionSummary { get; private set; } = new();
    public PhaseDiagramRegionSummary PipelineRegionSummary { get; private set; } = new();
    public PhaseDiagram? ParentDiagram { get; private set; }
    public string ParentDiagramName { get; private set; } = "-";
    public string AdaptiveSourceTypeText { get; private set; } = "--";
    public string AdaptiveReasonText { get; private set; } = "--";
    public string NextExplorationMessage { get; private set; } = "明確な探索候補は見つかっていません。別のパラメータ軸を選んでください。";
    public bool CanCreateBoundaryAdaptive { get; private set; }
    public bool CanCreateHighPipelineAdaptive { get; private set; }
    public bool CanCreateEmergentAdaptive { get; private set; }

    private HashSet<string> BoundaryCellKeys { get; } = new(StringComparer.Ordinal);
    private Dictionary<int, PhaseDiagramExperimentSummary> ExperimentSummariesByExperimentId { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadPageDataAsync(id))
        {
            return RedirectToPage("/PhaseDiagrams/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnGetDownloadCsvAsync(int id)
    {
        if (!await LoadPageDataAsync(id, recalcRunningStatus: false))
        {
            return RedirectToPage("/PhaseDiagrams/Index");
        }

        var csv = interpretationService.BuildPhaseDiagramCsv(
            Points,
            Diagram?.XParameterName ?? "X",
            Diagram?.YParameterName ?? "Y");
        var bytes = new UTF8Encoding(true).GetBytes(csv);
        return File(bytes, "text/csv; charset=utf-8", $"phase-diagram-{id}.csv");
    }

    public async Task<IActionResult> OnPostCreateAdaptiveFromBoundaryAsync(int id)
    {
        return await CreateAdaptiveSweepAsync(id, "Boundary");
    }

    public async Task<IActionResult> OnPostCreateAdaptiveFromHighPipelineAsync(int id)
    {
        return await CreateAdaptiveSweepAsync(id, "HighPipeline");
    }

    public async Task<IActionResult> OnPostCreateAdaptiveFromEmergentRegionAsync(int id)
    {
        return await CreateAdaptiveSweepAsync(id, "EmergentRegion");
    }

    private async Task<bool> LoadPageDataAsync(int id, bool recalcRunningStatus = true)
    {
        Diagram = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == id);
        if (Diagram is null)
        {
            return false;
        }

        if (recalcRunningStatus && Diagram.Status == PhaseDiagramStatus.Running)
        {
            try
            {
                await phaseDiagramRunner.RecalculateStatusAsync(Diagram.Id);
            }
            catch
            {
                // Reuse persisted status when recalculation fails.
            }

            Diagram = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == id);
            if (Diagram is null)
            {
                return false;
            }
        }

        await LoadParentDiagramAsync();

        ScenarioName = await db.Scenarios
            .Where(item => item.Id == Diagram.BaseScenarioId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync() ?? "-";

        Points = await db.PhaseDiagramPoints
            .Where(item => item.PhaseDiagramId == id)
            .OrderBy(item => item.YValue)
            .ThenBy(item => item.XValue)
            .ToListAsync();

        XValues = BuildParameterValues(Diagram.XStartValue, Diagram.XEndValue, Diagram.XStepValue);
        YValues = BuildParameterValues(Diagram.YStartValue, Diagram.YEndValue, Diagram.YStepValue);

        var experimentIds = Points
            .Where(item => item.ExperimentId.HasValue)
            .Select(item => item.ExperimentId!.Value)
            .Distinct()
            .ToList();

        ExperimentSummariesByExperimentId = experimentIds.Count == 0
            ? []
            : await BuildExperimentSummariesAsync(experimentIds);

        BuildRegionSummaries();
        BuildBoundaryAnalysis();
        BuildSummary();
        BuildHeatmaps();
        BuildPointResults();
        BuildAdaptiveProposal();

        var interpretation = interpretationService.BuildPhaseDiagramInterpretation(Points, ExperimentSummariesByExperimentId);
        SummaryCards = interpretation.SummaryCards;
        RecommendedInterpretation = interpretation.RecommendedRegion;
        RecommendedRegionText = interpretation.RecommendedRegion;
        DangerRegionText = interpretation.DangerRegion;
        AnalysisComment = interpretation.AutoComment;
        TransitionCandidateRows = interpretation.TransitionCandidates;
        BottleneckAnalysisRows = interpretation.BottleneckRows;

        return true;
    }

    private async Task<Dictionary<int, PhaseDiagramExperimentSummary>> BuildExperimentSummariesAsync(IReadOnlyCollection<int> experimentIds)
    {
        var runs = await db.ExperimentRuns
            .Where(item => experimentIds.Contains(item.ExperimentId))
            .ToListAsync();

        return runs
            .GroupBy(item => item.ExperimentId)
            .ToDictionary(
                group => group.Key,
                group => new PhaseDiagramExperimentSummary
                {
                    AverageComponentCount = group.Count() == 0 ? 0 : Math.Round(group.Average(item => item.ComponentCount), 3),
                    AverageShareInfoRate = group.Count() == 0 ? 0 : Math.Round(group.Average(item => item.ShareInfoRate), 3),
                    AverageProposeIdeaRate = group.Count() == 0 ? 0 : Math.Round(group.Average(item => item.ProposeIdeaRate), 3),
                    AverageCriticizeSupportRatio = group.Count() == 0 ? 0 : Math.Round(group.Average(item => item.CriticizeSupportRatio), 3)
                });
    }

    private async Task<IActionResult> CreateAdaptiveSweepAsync(int id, string adaptiveSourceType)
    {
        try
        {
            var diagram = adaptiveSourceType switch
            {
                "Boundary" => await phaseDiagramRunner.CreateAdaptiveBoundarySweepAsync(id),
                "HighPipeline" => await phaseDiagramRunner.CreateAdaptiveHighPipelineSweepAsync(id),
                "EmergentRegion" => await phaseDiagramRunner.CreateAdaptiveEmergentSweepAsync(id),
                _ => null
            };

            if (diagram is null)
            {
                DiagramMessage = "相図が見つからないため、再探索を作成できませんでした。";
                return RedirectToPage("/PhaseDiagrams/Details", new { id });
            }

            DiagramMessage = "Adaptive Sweep を作成しました。";
            return RedirectToPage("/PhaseDiagrams/Details", new { id = diagram.Id });
        }
        catch (InvalidOperationException ex)
        {
            DiagramMessage = ex.Message;
        }
        catch
        {
            DiagramMessage = "Adaptive Sweep の作成中にエラーが発生しました。";
        }

        return RedirectToPage("/PhaseDiagrams/Details", new { id });
    }

    private void BuildSummary()
    {
        if (Diagram is null || Points.Count == 0)
        {
            SummaryCards =
            [
                new() { Label = "最多相", Value = "--" },
                new() { Label = "最大 Emergent Rate", Value = "--" },
                new() { Label = "最大 Pipeline Completion", Value = "--" },
                new() { Label = "相境界候補数", Value = "0" },
                new() { Label = "Emergent 点数", Value = "0" },
                new() { Label = "高 Pipeline 点数", Value = "0" },
                new() { Label = "最頻 Bottleneck", Value = "--" },
                new() { Label = "推奨再探索領域", Value = RecommendedReseekArea },
                new() { Label = "最大 Average Trust", Value = "0.00" },
                new() { Label = "最大 Effective Density", Value = "0.00" },
                new() { Label = "最大 Serendipity Rate", Value = "0.00" },
                new() { Label = "最大 Knowledge Reconfiguration", Value = "0.00" }
            ];
            RecommendedInterpretation = "この相図にはまだ実行結果がありません。実行後に相図の傾向を表示します。";
            return;
        }

        var dominantPhase = Points
            .GroupBy(item => item.DominantPhase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .FirstOrDefault()?.Key ?? "--";

        var maxEmergent = Points
            .OrderByDescending(item => item.EmergentRate)
            .ThenBy(item => item.XValue)
            .ThenBy(item => item.YValue)
            .FirstOrDefault();

        var maxCompletion = Points
            .OrderByDescending(item => item.AveragePipelineCompletionScore)
            .ThenBy(item => item.XValue)
            .ThenBy(item => item.YValue)
            .FirstOrDefault();

        var mostCommonBottleneck = Points
            .Select(item => string.IsNullOrWhiteSpace(item.DominantBottleneck) ? "--" : item.DominantBottleneck)
            .GroupBy(item => item)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .FirstOrDefault()?.Key ?? "--";

        var maxTrust = Points.Max(item => item.AverageTrust);
        var maxDensity = Points.Max(item => item.AverageEffectiveDensity);
        var maxSerendipity = Points.Max(item => item.AverageSerendipityRate);
        var maxReconfiguration = Points.Max(item => item.AverageKnowledgeReconfigurationScore);

        SummaryCards =
        [
            new() { Label = "最多相", Value = FormatPhaseDisplay(dominantPhase) },
            new() { Label = "最大 Emergent Rate", Value = maxEmergent is null ? "--" : $"{maxEmergent.EmergentRate:0.00} @ ({maxEmergent.XValue:0.00}, {maxEmergent.YValue:0.00})" },
            new() { Label = "最大 Pipeline Completion", Value = maxCompletion is null ? "--" : $"{maxCompletion.AveragePipelineCompletionScore:0.00} @ ({maxCompletion.XValue:0.00}, {maxCompletion.YValue:0.00})" },
            new() { Label = "相境界候補数", Value = BoundaryCandidateCount.ToString(CultureInfo.InvariantCulture) },
            new() { Label = "Emergent 点数", Value = EmergentRegionSummary.Count.ToString(CultureInfo.InvariantCulture) },
            new() { Label = "高 Pipeline 点数", Value = PipelineRegionSummary.Count.ToString(CultureInfo.InvariantCulture) },
            new() { Label = "最頻 Bottleneck", Value = mostCommonBottleneck },
            new() { Label = "推奨再探索領域", Value = RecommendedReseekArea },
            new() { Label = "最大 Average Trust", Value = maxTrust.ToString("0.00") },
            new() { Label = "最大 Effective Density", Value = maxDensity.ToString("0.00") },
            new() { Label = "最大 Serendipity Rate", Value = maxSerendipity.ToString("0.00") },
            new() { Label = "最大 Knowledge Reconfiguration", Value = maxReconfiguration.ToString("0.00") }
        ];

        RecommendedInterpretation = BuildInterpretation(
            dominantPhase,
            maxEmergent?.EmergentRate ?? 0,
            maxCompletion?.AveragePipelineCompletionScore ?? 0,
            mostCommonBottleneck,
            maxDensity);
    }

    private void BuildRegionSummaries()
    {
        EmergentRegionSummary = BuildRegionSummary(
            "創発領域の簡易条件",
            "この相図では創発相は確認されていません。Pipeline Completion と Bottleneck Map を確認してください。",
            Points.Where(item => item.EmergentRate > 0 || string.Equals(item.DominantPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase)));

        PipelineRegionSummary = BuildRegionSummary(
            "創発直前領域の簡易条件",
            "この相図では創発直前領域は確認されていません。",
            Points.Where(item => item.AveragePipelineCompletionScore >= 0.70));

        if (EmergentRegionSummary.HasData)
        {
            RecommendedReseekArea = "Emergent領域の周辺を刻み幅を小さくして再探索してください。";
        }
        else if (PipelineRegionSummary.HasData)
        {
            RecommendedReseekArea = "高Pipeline領域周辺で、Bottleneckになっているパラメータを追加調整してください。";
        }
        else
        {
            RecommendedReseekArea = "まず信頼形成・知識多様性・セレンディピティのいずれかを強める条件で再探索してください。";
        }
    }

    private static PhaseDiagramRegionSummary BuildRegionSummary(
        string title,
        string emptyMessage,
        IEnumerable<PhaseDiagramPoint> source)
    {
        var points = source.ToList();
        if (points.Count == 0)
        {
            return new PhaseDiagramRegionSummary
            {
                Title = title,
                EmptyMessage = emptyMessage,
                Interpretation = emptyMessage
            };
        }

        var xValues = points.Select(item => item.XValue).ToList();
        var yValues = points.Select(item => item.YValue).ToList();
        var averagePipelineCompletionScore = points.Average(item => item.AveragePipelineCompletionScore);
        var mostCommonBottleneck = points
            .Select(item => string.IsNullOrWhiteSpace(item.DominantBottleneck) ? "--" : item.DominantBottleneck)
            .GroupBy(item => item)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .FirstOrDefault()?.Key ?? "--";

        return new PhaseDiagramRegionSummary
        {
            Title = title,
            EmptyMessage = emptyMessage,
            Count = points.Count,
            XMin = xValues.Min(),
            XMax = xValues.Max(),
            YMin = yValues.Min(),
            YMax = yValues.Max(),
            AverageX = xValues.Average(),
            AverageY = yValues.Average(),
            AveragePipelineCompletionScore = averagePipelineCompletionScore,
            MostCommonBottleneck = mostCommonBottleneck,
            Interpretation = BuildRegionInterpretation(title, points.Count, averagePipelineCompletionScore, mostCommonBottleneck)
        };
    }

    private static string BuildRegionInterpretation(string title, int count, double averagePipelineCompletionScore, string bottleneck)
    {
        if (string.Equals(title, "創発領域の簡易条件", StringComparison.Ordinal))
        {
            return $"この相図では、創発点が {count} 個見つかっています。X/Y の周辺を細かく再探索すると境界が見えやすくなります。";
        }

        if (string.Equals(title, "創発直前領域の簡易条件", StringComparison.Ordinal))
        {
            return $"この領域では平均 Pipeline Completion が {averagePipelineCompletionScore:0.00} です。Bottleneck は {bottleneck} で、ここを改善すると創発へ移行する可能性があります。";
        }

        return "--";
    }

    private async Task LoadParentDiagramAsync()
    {
        ParentDiagram = null;
        ParentDiagramName = "-";

        if (Diagram?.ParentPhaseDiagramId is null)
        {
            return;
        }

        ParentDiagram = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == Diagram.ParentPhaseDiagramId.Value);
        ParentDiagramName = ParentDiagram?.Name ?? "-";
    }

    private void BuildAdaptiveProposal()
    {
        CanCreateBoundaryAdaptive = BoundaryCandidateCount > 0;
        CanCreateHighPipelineAdaptive = PipelineRegionSummary.HasData;
        CanCreateEmergentAdaptive = EmergentRegionSummary.HasData;

        AdaptiveSourceTypeText = FormatAdaptiveSourceType(Diagram?.AdaptiveSourceType);
        AdaptiveReasonText = string.IsNullOrWhiteSpace(Diagram?.AdaptiveReason) ? "--" : Diagram.AdaptiveReason!;

        if (CanCreateEmergentAdaptive)
        {
            NextExplorationMessage = "創発領域が見つかっています。創発領域周辺を細かく再探索してください。";
            return;
        }

        if (CanCreateHighPipelineAdaptive)
        {
            NextExplorationMessage = "創発直前領域があります。高Pipeline領域周辺を細かく再探索してください。";
            return;
        }

        if (CanCreateBoundaryAdaptive)
        {
            NextExplorationMessage = "相境界候補があります。相境界周辺を細かく再探索してください。";
            return;
        }

        NextExplorationMessage = "明確な探索候補は見つかっていません。別のパラメータ軸を選んでください。";
    }

    private static string FormatAdaptiveSourceType(string? adaptiveSourceType) => adaptiveSourceType switch
    {
        "Boundary" => "相境界",
        "HighPipeline" => "高Pipeline",
        "EmergentRegion" => "創発領域",
        "Manual" => "手動",
        _ => string.IsNullOrWhiteSpace(adaptiveSourceType) ? "--" : adaptiveSourceType.Trim()
    };

    private void BuildBoundaryAnalysis()
    {
        BoundaryEntries = [];
        BoundaryCellKeys.Clear();
        BoundaryCandidateCount = 0;
        EmergentBoundaryCount = 0;
        StableBoundaryCount = 0;
        SiloBoundaryCount = 0;
        MostCommonBoundaryPattern = "--";

        if (Diagram is null || Points.Count == 0 || XValues.Count == 0 || YValues.Count == 0)
        {
            return;
        }

        var pointMap = Points.ToDictionary(item => GetPointKey(item.XValue, item.YValue));
        var orderedYValues = YValues.OrderByDescending(item => item).ToList();

        for (var yIndex = 0; yIndex < orderedYValues.Count; yIndex++)
        {
            var yValue = orderedYValues[yIndex];
            for (var xIndex = 0; xIndex < XValues.Count; xIndex++)
            {
                var xValue = XValues[xIndex];
                if (!pointMap.TryGetValue(GetPointKey(xValue, yValue), out var current))
                {
                    continue;
                }

                if (xIndex + 1 < XValues.Count &&
                    pointMap.TryGetValue(GetPointKey(XValues[xIndex + 1], yValue), out var right) &&
                    !AreSamePhase(current.DominantPhase, right.DominantPhase))
                {
                    AddBoundary(current, right, "Horizontal");
                }

                if (yIndex + 1 < orderedYValues.Count &&
                    pointMap.TryGetValue(GetPointKey(xValue, orderedYValues[yIndex + 1]), out var down) &&
                    !AreSamePhase(current.DominantPhase, down.DominantPhase))
                {
                    AddBoundary(current, down, "Vertical");
                }
            }
        }

        BoundaryEntries = BoundaryEntries
            .OrderBy(item => item.YValue)
            .ThenBy(item => item.XValue)
            .ThenBy(item => item.Direction, StringComparer.Ordinal)
            .ToList();

        BoundaryCandidateCount = BoundaryEntries.Count;
        EmergentBoundaryCount = BoundaryEntries.Count(item =>
            string.Equals(NormalizePhase(item.FromPhase), SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizePhase(item.ToPhase), SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase));
        StableBoundaryCount = BoundaryEntries.Count(item =>
            string.Equals(NormalizePhase(item.FromPhase), SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizePhase(item.ToPhase), SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase));
        SiloBoundaryCount = BoundaryEntries.Count(item =>
            string.Equals(NormalizePhase(item.FromPhase), SimulationPhase.Silo, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizePhase(item.ToPhase), SimulationPhase.Silo, StringComparison.OrdinalIgnoreCase));

        MostCommonBoundaryPattern = BoundaryEntries.Count == 0
            ? "--"
            : BoundaryEntries
                .GroupBy(item => $"{FormatPhaseDisplay(item.FromPhase)} → {FormatPhaseDisplay(item.ToPhase)}")
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .First()
                .Key;
    }

    private void AddBoundary(PhaseDiagramPoint current, PhaseDiagramPoint neighbor, string direction)
    {
        BoundaryEntries.Add(new PhaseDiagramBoundaryEntry
        {
            FromPhase = NormalizePhase(current.DominantPhase),
            ToPhase = NormalizePhase(neighbor.DominantPhase),
            XValue = current.XValue,
            YValue = current.YValue,
            NeighborXValue = neighbor.XValue,
            NeighborYValue = neighbor.YValue,
            Direction = direction
        });

        BoundaryCellKeys.Add(GetPointKey(current.XValue, current.YValue));
        BoundaryCellKeys.Add(GetPointKey(neighbor.XValue, neighbor.YValue));
    }

    private void BuildHeatmaps()
    {
        DominantPhaseRows = BuildRows(point => new PhaseDiagramHeatmapCell
        {
            XValue = point.XValue,
            YValue = point.YValue,
            ExperimentId = point.ExperimentId,
            Text = FormatPhaseShort(point.DominantPhase),
            CssClass = GetPhaseCellClass(point.DominantPhase),
            Tooltip = FormatPhaseDisplay(point.DominantPhase)
        });

        EmergentRateRows = BuildRows(point => new PhaseDiagramHeatmapCell
        {
            XValue = point.XValue,
            YValue = point.YValue,
            ExperimentId = point.ExperimentId,
            Text = point.EmergentRate.ToString("0.00"),
            CssClass = GetEmergentRateCellClass(point.EmergentRate),
            Tooltip = $"EmergentRate {point.EmergentRate:0.00}"
        });

        PipelineCompletionRows = BuildRows(point => new PhaseDiagramHeatmapCell
        {
            XValue = point.XValue,
            YValue = point.YValue,
            ExperimentId = point.ExperimentId,
            Text = point.AveragePipelineCompletionScore.ToString("0.00"),
            CssClass = GetPipelineCompletionCellClass(point.AveragePipelineCompletionScore),
            Tooltip = $"PipelineCompletion {point.AveragePipelineCompletionScore:0.00}"
        });

        BottleneckRows = BuildRows(point => new PhaseDiagramHeatmapCell
        {
            XValue = point.XValue,
            YValue = point.YValue,
            ExperimentId = point.ExperimentId,
            Text = FormatBottleneckShort(point.DominantBottleneck),
            CssClass = GetBottleneckCellClass(point.DominantBottleneck),
            Tooltip = BuildBottleneckInterpretation(point.DominantBottleneck)
        });
    }

    private void BuildPointResults()
    {
        PointResults = Points
            .OrderBy(item => item.YValue)
            .ThenBy(item => item.XValue)
            .Select(item => new PhaseDiagramPointResultRow
            {
                XValue = item.XValue,
                YValue = item.YValue,
                ExperimentId = item.ExperimentId,
                DominantPhase = item.DominantPhase,
                EmergentRate = item.EmergentRate,
                StableRate = item.StableRate,
                LearningRate = item.LearningRate,
                SiloRate = item.SiloRate,
                AverageTrust = item.AverageTrust,
                AverageEffectiveDensity = item.AverageEffectiveDensity,
                AverageKnowledgeDiversity = item.AverageKnowledgeDiversity,
                AverageKnowledgeReconfigurationScore = item.AverageKnowledgeReconfigurationScore,
                AverageSerendipityRate = item.AverageSerendipityRate,
                AveragePipelineCompletionScore = item.AveragePipelineCompletionScore,
                DominantBottleneck = item.DominantBottleneck
            })
            .ToList();
    }

    private List<PhaseDiagramHeatmapRow> BuildRows(Func<PhaseDiagramPoint, PhaseDiagramHeatmapCell> selector)
    {
        List<PhaseDiagramHeatmapRow> rows = [];

        foreach (var yValue in YValues.OrderByDescending(item => item))
        {
            var row = new PhaseDiagramHeatmapRow { YValue = yValue };
            foreach (var xValue in XValues)
            {
                var point = Points.FirstOrDefault(item =>
                    Math.Abs(item.XValue - xValue) < 0.0005 &&
                    Math.Abs(item.YValue - yValue) < 0.0005);

                row.Cells.Add(point is null
                    ? new PhaseDiagramHeatmapCell
                    {
                        XValue = xValue,
                        YValue = yValue,
                        Text = "--",
                        CssClass = "phase-cell-unknown",
                        Tooltip = "データなし"
                    }
                    : BuildCell(point, selector));
            }

            rows.Add(row);
        }

        return rows;
    }

    private PhaseDiagramHeatmapCell BuildCell(PhaseDiagramPoint point, Func<PhaseDiagramPoint, PhaseDiagramHeatmapCell> selector)
    {
        var cell = selector(point);
        cell.IsBoundary = BoundaryCellKeys.Contains(GetPointKey(point.XValue, point.YValue));
        return cell;
    }

    public string FormatStatus(string? status) => status switch
    {
        PhaseDiagramStatus.Created => "Created（作成済み）",
        PhaseDiagramStatus.Running => "Running（実行中）",
        PhaseDiagramStatus.Completed => "Completed（完了）",
        PhaseDiagramStatus.Failed => "Failed（失敗）",
        _ => status ?? "-"
    };

    public string FormatPhaseDisplay(string? phase) => phase switch
    {
        SimulationPhase.Forming => "Forming（形成期）",
        SimulationPhase.Learning => "Learning（学習期）",
        SimulationPhase.Stable => "Stable（安定期）",
        SimulationPhase.Adaptation => "Adaptation（適応期）",
        SimulationPhase.Emergent => "Emergent（創発期）",
        SimulationPhase.Silo => "Silo（サイロ化）",
        SimulationPhase.Chaos => "Chaos（混沌期）",
        SimulationPhase.Collapse => "Collapse（崩壊期）",
        _ => phase ?? "--"
    };

    public string GetPointRowClass(PhaseDiagramPointResultRow row) => row.DominantPhase switch
    {
        SimulationPhase.Emergent => "phase-row-emergent",
        SimulationPhase.Silo => "phase-row-silo",
        SimulationPhase.Chaos => "phase-row-chaos",
        _ => string.Empty
    };

    public string FormatBoundaryPhase(string? phase) => FormatPhaseDisplay(phase);

    public string FormatDirection(string direction) => direction switch
    {
        "Horizontal" => "横方向",
        "Vertical" => "縦方向",
        _ => direction
    };

    public string FormatRange(double? min, double? max) => min.HasValue && max.HasValue
        ? $"{min.Value:0.00}〜{max.Value:0.00}"
        : "--";

    public string FormatCenter(double? xValue, double? yValue) => xValue.HasValue && yValue.HasValue
        ? $"X={xValue.Value:0.00}, Y={yValue.Value:0.00}"
        : "--";

    public string FormatNumber(double? value) => value.HasValue ? value.Value.ToString("0.00") : "--";

    private static string BuildInterpretation(string dominantPhase, double maxEmergentRate, double maxCompletion, string bottleneck, double maxDensity)
    {
        if (maxEmergentRate > 0)
        {
            return "この相図では創発相が発生する領域が確認できます。該当座標の周辺を細かく再探索してください。";
        }

        if (maxCompletion >= 0.7)
        {
            return "創発プロセスはかなり進んでいますが、最終的な相変化には届いていません。Bottleneck Map を確認してください。";
        }

        if (string.Equals(dominantPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase))
        {
            return "多くの条件で学習相に留まっています。信頼形成、知識再構成、適応への変換が不足している可能性があります。";
        }

        if (string.Equals(dominantPhase, SimulationPhase.Silo, StringComparison.OrdinalIgnoreCase))
        {
            return "多くの条件でネットワーク分断が優勢です。信頼閾値や情報共有条件を見直してください。";
        }

        if (maxDensity < 0.3)
        {
            return "実効ネットワーク密度が全体的に低く、協働ネットワークが十分に形成されていません。";
        }

        return $"多くの条件で {dominantPhase} が支配的です。現在の主なボトルネックは {bottleneck} です。";
    }

    private static string GetPhaseCellClass(string phase) => phase switch
    {
        SimulationPhase.Emergent => "phase-cell-emergent",
        SimulationPhase.Stable => "phase-cell-stable",
        SimulationPhase.Learning => "phase-cell-learning",
        SimulationPhase.Silo => "phase-cell-silo",
        SimulationPhase.Chaos => "phase-cell-chaos",
        SimulationPhase.Collapse => "phase-cell-collapse",
        SimulationPhase.Adaptation => "phase-cell-adaptation",
        SimulationPhase.Forming => "phase-cell-forming",
        _ => "phase-cell-unknown"
    };

    private static string GetEmergentRateCellClass(double value) => value switch
    {
        >= 0.75 => "rate-cell-max",
        >= 0.50 => "rate-cell-high",
        >= 0.25 => "rate-cell-mid",
        > 0.00 => "rate-cell-low",
        _ => "rate-cell-0"
    };

    private static string GetPipelineCompletionCellClass(double value) => value switch
    {
        >= 0.75 => "pipeline-cell-max",
        >= 0.50 => "pipeline-cell-high",
        >= 0.25 => "pipeline-cell-mid",
        > 0.00 => "pipeline-cell-low",
        _ => "pipeline-cell-0"
    };

    private static string GetBottleneckCellClass(string bottleneck) => bottleneck switch
    {
        "Serendipity" => "bottleneck-cell-serendipity",
        "KnowledgeRecombination" => "bottleneck-cell-recombination",
        "KnowledgeReconfiguration" => "bottleneck-cell-reconfiguration",
        "Learning" => "bottleneck-cell-learning",
        "Adaptation" => "bottleneck-cell-adaptation",
        "Emergence" => "bottleneck-cell-emergence",
        _ => "bottleneck-cell-unknown"
    };

    private static string FormatPhaseShort(string phase) => phase switch
    {
        SimulationPhase.Emergent => "Emg",
        SimulationPhase.Stable => "Sta",
        SimulationPhase.Learning => "Lrn",
        SimulationPhase.Silo => "Silo",
        SimulationPhase.Chaos => "Chs",
        SimulationPhase.Collapse => "Col",
        SimulationPhase.Adaptation => "Adp",
        SimulationPhase.Forming => "Frm",
        _ => "--"
    };

    private static string FormatBottleneckShort(string bottleneck) => bottleneck switch
    {
        "KnowledgeRecombination" => "KRec",
        "KnowledgeReconfiguration" => "KCfg",
        "Serendipity" => "Ser",
        "Learning" => "Lrn",
        "Adaptation" => "Adp",
        "Emergence" => "Emg",
        _ => "--"
    };

    private static string BuildBottleneckInterpretation(string? bottleneck) => bottleneck switch
    {
        "Serendipity" => "偶然の有用結合が弱く、知識再結合の起点が不足しています。",
        "KnowledgeRecombination" => "セレンディピティは起きていますが、知識の組み合わせ直しに届いていません。",
        "KnowledgeReconfiguration" => "知識再結合はありますが、組織全体の知識構造の再編に届いていません。",
        "Learning" => "知識構造は変化していますが、継続的な学習相として安定していません。",
        "Adaptation" => "学習は進んでいますが、環境変化への適応行動に変換されていません。",
        "Emergence" => "前段階はそろっていますが、システム全体の相変化としてはまだ現れていません。",
        _ => "--"
    };

    private static string NormalizePhase(string? phase)
    {
        return string.IsNullOrWhiteSpace(phase) ? "--" : phase.Trim();
    }

    private static bool AreSamePhase(string? left, string? right)
    {
        return string.Equals(NormalizePhase(left), NormalizePhase(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPointKey(double xValue, double yValue)
        => $"{Math.Round(xValue, 3).ToString("0.000", CultureInfo.InvariantCulture)}|{Math.Round(yValue, 3).ToString("0.000", CultureInfo.InvariantCulture)}";

    private static List<double> BuildParameterValues(double startValue, double endValue, double stepValue)
    {
        List<double> values = [];
        var start = decimal.Round((decimal)startValue, 3);
        var end = decimal.Round((decimal)endValue, 3);
        var step = decimal.Round((decimal)stepValue, 3);

        for (var value = start; value <= end + 0.0001m; value += step)
        {
            values.Add(Math.Round((double)value, 3));
        }

        return values;
    }
}
