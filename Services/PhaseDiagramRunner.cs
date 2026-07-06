using System.Globalization;
using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Services;

public sealed class PhaseDiagramRunner(
    AppDbContext db,
    IExperimentExecutionService experimentExecutionService,
    SimulationParameterApplier parameterApplier)
{
    private const double MinimumAdaptiveStep = 0.005;
    private const int MaximumAdaptiveGridPoints = 100;
    private const double HighPipelineThreshold = 0.70;

    public async Task<PhaseDiagram?> RunAsync(int phaseDiagramId, CancellationToken cancellationToken = default)
    {
        var diagram = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == phaseDiagramId, cancellationToken);
        if (diagram is null)
        {
            return null;
        }

        if (diagram.Status == PhaseDiagramStatus.Running)
        {
            return diagram;
        }

        var baseScenario = await db.Scenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == diagram.BaseScenarioId, cancellationToken);

        if (baseScenario is null)
        {
            diagram.Status = PhaseDiagramStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            return diagram;
        }

        var xValues = BuildParameterValues(diagram.XStartValue, diagram.XEndValue, diagram.XStepValue);
        var yValues = BuildParameterValues(diagram.YStartValue, diagram.YEndValue, diagram.YStepValue);

        diagram.Status = PhaseDiagramStatus.Running;
        diagram.CompletedAt = null;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await DeleteExistingResultsAsync(diagram.Id, cancellationToken);

            foreach (var xValue in xValues)
            {
                foreach (var yValue in yValues)
                {
                    var experiment = SimulationFactory.CreateExperimentFromScenario(
                        baseScenario,
                        $"{diagram.Name} X={diagram.XParameterDisplayName}:{xValue:0.000} / Y={diagram.YParameterDisplayName}:{yValue:0.000}",
                        $"{diagram.Description} / X={diagram.XParameterDisplayName}:{xValue:0.000} / Y={diagram.YParameterDisplayName}:{yValue:0.000}",
                        diagram.RunsPerPoint,
                        diagram.AgentCount,
                        diagram.TotalSteps,
                        diagram.LlmProvider,
                        diagram.LlmModel);

                    if (!parameterApplier.ApplyParameter(experiment, diagram.XParameterName, xValue))
                    {
                        throw new InvalidOperationException($"Unsupported X parameter: {diagram.XParameterName}");
                    }

                    if (!parameterApplier.ApplyParameter(experiment, diagram.YParameterName, yValue))
                    {
                        throw new InvalidOperationException($"Unsupported Y parameter: {diagram.YParameterName}");
                    }

                    db.Experiments.Add(experiment);
                    await db.SaveChangesAsync(cancellationToken);

                    await experimentExecutionService.RunExperimentAsync(experiment.Id, "Minimal", cancellationToken);

                    var point = await BuildPointAsync(diagram.Id, experiment.Id, xValue, yValue, cancellationToken);
                    db.PhaseDiagramPoints.Add(point);
                    await db.SaveChangesAsync(cancellationToken);
                }
            }

            diagram.Status = PhaseDiagramStatus.Completed;
            diagram.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            diagram.Status = PhaseDiagramStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }

        return diagram;
    }

    public Task<PhaseDiagram?> CreateAdaptiveBoundarySweepAsync(int parentPhaseDiagramId, CancellationToken cancellationToken = default)
        => CreateAdaptiveSweepAsync(parentPhaseDiagramId, "Boundary", cancellationToken);

    public Task<PhaseDiagram?> CreateAdaptiveHighPipelineSweepAsync(int parentPhaseDiagramId, CancellationToken cancellationToken = default)
        => CreateAdaptiveSweepAsync(parentPhaseDiagramId, "HighPipeline", cancellationToken);

    public Task<PhaseDiagram?> CreateAdaptiveEmergentSweepAsync(int parentPhaseDiagramId, CancellationToken cancellationToken = default)
        => CreateAdaptiveSweepAsync(parentPhaseDiagramId, "EmergentRegion", cancellationToken);

    public async Task<PhaseDiagram?> RecalculateStatusAsync(int phaseDiagramId, CancellationToken cancellationToken = default)
    {
        var diagram = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == phaseDiagramId, cancellationToken);
        if (diagram is null)
        {
            return null;
        }

        if (diagram.Status == PhaseDiagramStatus.Failed || diagram.Status == PhaseDiagramStatus.Completed)
        {
            return diagram;
        }

        var xCount = BuildParameterValues(diagram.XStartValue, diagram.XEndValue, diagram.XStepValue).Count;
        var yCount = BuildParameterValues(diagram.YStartValue, diagram.YEndValue, diagram.YStepValue).Count;
        var expectedPoints = xCount * yCount;

        var points = await db.PhaseDiagramPoints
            .Where(item => item.PhaseDiagramId == diagram.Id)
            .Select(item => new { item.ExperimentId })
            .ToListAsync(cancellationToken);

        if (points.Count == 0)
        {
            diagram.Status = PhaseDiagramStatus.Created;
            await db.SaveChangesAsync(cancellationToken);
            return diagram;
        }

        var experimentIds = points
            .Where(item => item.ExperimentId.HasValue)
            .Select(item => item.ExperimentId!.Value)
            .Distinct()
            .ToList();

        var experiments = experimentIds.Count == 0
            ? []
            : await db.Experiments
                .Where(item => experimentIds.Contains(item.Id))
                .Select(item => new { item.Id, item.Status })
                .ToListAsync(cancellationToken);

        if (experiments.Any(item => string.Equals(item.Status, "Failed", StringComparison.OrdinalIgnoreCase)))
        {
            diagram.Status = PhaseDiagramStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            return diagram;
        }

        var completed = points.Count >= expectedPoints
            && experiments.Count >= expectedPoints
            && experiments.All(item => string.Equals(item.Status, ExperimentStatus.Completed, StringComparison.OrdinalIgnoreCase));

        diagram.Status = completed ? PhaseDiagramStatus.Completed : PhaseDiagramStatus.Running;
        if (completed && !diagram.CompletedAt.HasValue)
        {
            diagram.CompletedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return diagram;
    }

    public async Task<AdaptivePlan?> BuildAdaptivePlanAsync(
        int parentPhaseDiagramId,
        string adaptiveSourceType,
        CancellationToken cancellationToken = default)
    {
        var parent = await db.PhaseDiagrams
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == parentPhaseDiagramId, cancellationToken);

        if (parent is null)
        {
            return null;
        }

        var points = await db.PhaseDiagramPoints
            .Where(item => item.PhaseDiagramId == parentPhaseDiagramId)
            .ToListAsync(cancellationToken);

        if (points.Count == 0)
        {
            throw new InvalidOperationException("再探索用の点データがありません。");
        }

        return adaptiveSourceType switch
        {
            "Boundary" => BuildBoundaryAdaptivePlan(parent, points),
            "HighPipeline" => BuildHighPipelineAdaptivePlan(parent, points),
            "EmergentRegion" => BuildEmergentAdaptivePlan(parent, points),
            "Manual" => BuildManualAdaptivePlan(parent),
            _ => throw new InvalidOperationException("未対応の再探索タイプです。")
        };
    }

    private async Task<PhaseDiagram?> CreateAdaptiveSweepAsync(
        int parentPhaseDiagramId,
        string adaptiveSourceType,
        CancellationToken cancellationToken)
    {
        var parent = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == parentPhaseDiagramId, cancellationToken);
        if (parent is null)
        {
            return null;
        }

        var plan = await BuildAdaptivePlanAsync(parentPhaseDiagramId, adaptiveSourceType, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var diagram = new PhaseDiagram
        {
            Name = plan.Name,
            Description = plan.Description,
            BaseScenarioId = parent.BaseScenarioId,
            ParentPhaseDiagramId = parent.Id,
            AdaptiveSourceType = plan.AdaptiveSourceType,
            AdaptiveReason = plan.AdaptiveReason,
            IsAdaptiveSweep = true,
            XParameterName = parent.XParameterName,
            XParameterDisplayName = parent.XParameterDisplayName,
            XStartValue = plan.XStartValue,
            XEndValue = plan.XEndValue,
            XStepValue = plan.XStepValue,
            YParameterName = parent.YParameterName,
            YParameterDisplayName = parent.YParameterDisplayName,
            YStartValue = plan.YStartValue,
            YEndValue = plan.YEndValue,
            YStepValue = plan.YStepValue,
            RunsPerPoint = Math.Max(3, parent.RunsPerPoint),
            AgentCount = parent.AgentCount,
            TotalSteps = parent.TotalSteps,
            LlmProvider = parent.LlmProvider,
            LlmModel = parent.LlmModel,
            Status = PhaseDiagramStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        db.PhaseDiagrams.Add(diagram);
        await db.SaveChangesAsync(cancellationToken);
        return diagram;
    }

    private static AdaptivePlan BuildBoundaryAdaptivePlan(PhaseDiagram parent, IReadOnlyCollection<PhaseDiagramPoint> points)
    {
        var xValues = BuildParameterValues(parent.XStartValue, parent.XEndValue, parent.XStepValue);
        var yValues = BuildParameterValues(parent.YStartValue, parent.YEndValue, parent.YStepValue);
        var pointMap = points.ToDictionary(item => GetPointKey(item.XValue, item.YValue));
        var orderedYValues = yValues.OrderByDescending(item => item).ToList();
        var boundaryPoints = new List<PhaseDiagramPoint>();
        var boundaryTransitions = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var yIndex = 0; yIndex < orderedYValues.Count; yIndex++)
        {
            var yValue = orderedYValues[yIndex];
            for (var xIndex = 0; xIndex < xValues.Count; xIndex++)
            {
                var xValue = xValues[xIndex];
                if (!pointMap.TryGetValue(GetPointKey(xValue, yValue), out var current))
                {
                    continue;
                }

                if (xIndex + 1 < xValues.Count &&
                    pointMap.TryGetValue(GetPointKey(xValues[xIndex + 1], yValue), out var right) &&
                    !AreSamePhase(current.DominantPhase, right.DominantPhase))
                {
                    AddBoundarySelection(boundaryPoints, boundaryTransitions, current, right);
                }

                if (yIndex + 1 < orderedYValues.Count &&
                    pointMap.TryGetValue(GetPointKey(xValue, orderedYValues[yIndex + 1]), out var down) &&
                    !AreSamePhase(current.DominantPhase, down.DominantPhase))
                {
                    AddBoundarySelection(boundaryPoints, boundaryTransitions, current, down);
                }
            }
        }

        if (boundaryPoints.Count == 0)
        {
            throw new InvalidOperationException("相境界候補がないため、相境界周辺の再探索は作成できません。");
        }

        var mostCommonPattern = boundaryTransitions
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .First().Key;

        var plan = BuildAdaptivePlanFromPoints(
            parent,
            boundaryPoints,
            "Boundary",
            "Adaptive Sweep: 相境界周辺を細かく再探索します。",
            $"{parent.Name} - Adaptive Boundary",
            $"Adaptive Sweep: 相境界周辺を細かく再探索します。 {mostCommonPattern} を中心に再探索します。");

        return plan;
    }

    private static AdaptivePlan BuildHighPipelineAdaptivePlan(PhaseDiagram parent, IReadOnlyCollection<PhaseDiagramPoint> points)
    {
        var selected = points
            .Where(item => item.AveragePipelineCompletionScore >= HighPipelineThreshold)
            .OrderByDescending(item => item.AveragePipelineCompletionScore)
            .ThenBy(item => item.XValue)
            .ThenBy(item => item.YValue)
            .ToList();

        if (selected.Count == 0)
        {
            selected = points
                .OrderByDescending(item => item.AveragePipelineCompletionScore)
                .ThenBy(item => item.XValue)
                .ThenBy(item => item.YValue)
                .Take(Math.Max(1, (int)Math.Ceiling(points.Count * 0.2)))
                .ToList();
        }

        if (selected.Count == 0)
        {
            throw new InvalidOperationException("高Pipeline領域がないため、再探索は作成できません。");
        }

        return BuildAdaptivePlanFromPoints(
            parent,
            selected,
            "HighPipeline",
            "Adaptive Sweep: 高Pipeline領域を細かく再探索します。",
            $"{parent.Name} - Adaptive HighPipeline",
            "Adaptive Sweep: 高Pipeline領域を細かく再探索します。");
    }

    private static AdaptivePlan BuildEmergentAdaptivePlan(PhaseDiagram parent, IReadOnlyCollection<PhaseDiagramPoint> points)
    {
        var selected = points
            .Where(item => item.EmergentRate > 0 || string.Equals(item.DominantPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.EmergentRate)
            .ThenBy(item => item.XValue)
            .ThenBy(item => item.YValue)
            .ToList();

        if (selected.Count == 0)
        {
            throw new InvalidOperationException("創発領域がないため、創発領域周辺の再探索は作成できません。");
        }

        return BuildAdaptivePlanFromPoints(
            parent,
            selected,
            "EmergentRegion",
            "Adaptive Sweep: 創発領域周辺を細かく再探索します。",
            $"{parent.Name} - Adaptive Emergent",
            "Adaptive Sweep: 創発領域周辺を細かく再探索します。");
    }

    private static AdaptivePlan BuildManualAdaptivePlan(PhaseDiagram parent)
    {
        return new AdaptivePlan(
            $"{parent.Name} - Adaptive Manual",
            "Adaptive Sweep: 手動調整用の再探索テンプレートです。",
            "Manual",
            "手動で範囲を調整するためのテンプレートとして作成されました。",
            parent.XStartValue,
            parent.XEndValue,
            Math.Max(parent.XStepValue / 2, MinimumAdaptiveStep),
            parent.YStartValue,
            parent.YEndValue,
            Math.Max(parent.YStepValue / 2, MinimumAdaptiveStep));
    }

    private static AdaptivePlan BuildAdaptivePlanFromPoints(
        PhaseDiagram parent,
        IReadOnlyCollection<PhaseDiagramPoint> selectedPoints,
        string adaptiveSourceType,
        string description,
        string name,
        string adaptiveReason)
    {
        var xValues = selectedPoints.Select(item => Math.Round(item.XValue, 3)).Distinct().OrderBy(item => item).ToList();
        var yValues = selectedPoints.Select(item => Math.Round(item.YValue, 3)).Distinct().OrderBy(item => item).ToList();

        if (xValues.Count == 0 || yValues.Count == 0)
        {
            throw new InvalidOperationException("再探索用の座標を特定できませんでした。");
        }

        var xStart = Math.Max(parent.XStartValue, xValues.Min() - parent.XStepValue);
        var xEnd = Math.Min(parent.XEndValue, xValues.Max() + parent.XStepValue);
        var yStart = Math.Max(parent.YStartValue, yValues.Min() - parent.YStepValue);
        var yEnd = Math.Min(parent.YEndValue, yValues.Max() + parent.YStepValue);

        var xStep = Math.Max(parent.XStepValue / 2, MinimumAdaptiveStep);
        var yStep = Math.Max(parent.YStepValue / 2, MinimumAdaptiveStep);

        var adapted = LimitAdaptiveGrid(parent, xStart, xEnd, xStep, yStart, yEnd, yStep);

        return new AdaptivePlan(
            name,
            description,
            adaptiveSourceType,
            adaptiveReason,
            adapted.XStartValue,
            adapted.XEndValue,
            adapted.XStepValue,
            adapted.YStartValue,
            adapted.YEndValue,
            adapted.YStepValue);
    }

    private static (double XStartValue, double XEndValue, double XStepValue, double YStartValue, double YEndValue, double YStepValue) LimitAdaptiveGrid(
        PhaseDiagram parent,
        double xStartValue,
        double xEndValue,
        double xStepValue,
        double yStartValue,
        double yEndValue,
        double yStepValue)
    {
        var xStart = Math.Round(Math.Max(parent.XStartValue, xStartValue), 3);
        var xEnd = Math.Round(Math.Min(parent.XEndValue, xEndValue), 3);
        var yStart = Math.Round(Math.Max(parent.YStartValue, yStartValue), 3);
        var yEnd = Math.Round(Math.Min(parent.YEndValue, yEndValue), 3);
        var xStep = Math.Round(Math.Max(xStepValue, MinimumAdaptiveStep), 3);
        var yStep = Math.Round(Math.Max(yStepValue, MinimumAdaptiveStep), 3);

        var guard = 0;
        while (CalculatePointCount(xStart, xEnd, xStep) * CalculatePointCount(yStart, yEnd, yStep) > MaximumAdaptiveGridPoints && guard < 100)
        {
            guard++;

            var xSpan = xEnd - xStart;
            var ySpan = yEnd - yStart;
            if (xSpan <= 0 && ySpan <= 0)
            {
                break;
            }

            if (xSpan >= ySpan && xSpan > xStep)
            {
                xStart = Math.Round(Math.Min(xEnd, xStart + xStep), 3);
                xEnd = Math.Round(Math.Max(xStart, xEnd - xStep), 3);
            }
            else if (ySpan > yStep)
            {
                yStart = Math.Round(Math.Min(yEnd, yStart + yStep), 3);
                yEnd = Math.Round(Math.Max(yStart, yEnd - yStep), 3);
            }
            else
            {
                break;
            }

            xStart = Math.Max(parent.XStartValue, xStart);
            xEnd = Math.Min(parent.XEndValue, xEnd);
            yStart = Math.Max(parent.YStartValue, yStart);
            yEnd = Math.Min(parent.YEndValue, yEnd);

            if (xStart > xEnd || yStart > yEnd)
            {
                break;
            }
        }

        if (CalculatePointCount(xStart, xEnd, xStep) * CalculatePointCount(yStart, yEnd, yStep) > MaximumAdaptiveGridPoints)
        {
            throw new InvalidOperationException("再探索範囲が広すぎます。手動で範囲を狭めてください。");
        }

        return (xStart, xEnd, xStep, yStart, yEnd, yStep);
    }

    private static void AddBoundarySelection(
        ICollection<PhaseDiagramPoint> boundaryPoints,
        IDictionary<string, int> boundaryTransitions,
        PhaseDiagramPoint current,
        PhaseDiagramPoint neighbor)
    {
        boundaryPoints.Add(current);
        boundaryPoints.Add(neighbor);

        var transitionKey = $"{FormatPhaseLabel(current.DominantPhase)} → {FormatPhaseLabel(neighbor.DominantPhase)}";
        boundaryTransitions[transitionKey] = boundaryTransitions.TryGetValue(transitionKey, out var count) ? count + 1 : 1;
    }

    private async Task DeleteExistingResultsAsync(int phaseDiagramId, CancellationToken cancellationToken)
    {
        var existingPoints = await db.PhaseDiagramPoints
            .Where(item => item.PhaseDiagramId == phaseDiagramId)
            .ToListAsync(cancellationToken);

        var experimentIds = existingPoints
            .Where(item => item.ExperimentId.HasValue)
            .Select(item => item.ExperimentId!.Value)
            .Distinct()
            .ToList();

        foreach (var experimentId in experimentIds)
        {
            await SimulationDataDeletion.DeleteExperimentAsync(db, experimentId, cancellationToken);
        }

        if (existingPoints.Count > 0)
        {
            db.PhaseDiagramPoints.RemoveRange(existingPoints);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<PhaseDiagramPoint> BuildPointAsync(
        int phaseDiagramId,
        int experimentId,
        double xValue,
        double yValue,
        CancellationToken cancellationToken)
    {
        var experimentRuns = await db.ExperimentRuns
            .Where(item => item.ExperimentId == experimentId)
            .OrderBy(item => item.RunNo)
            .ToListAsync(cancellationToken);

        var projects = await db.SimulationProjects
            .Where(item => item.ExperimentId == experimentId)
            .Select(item => new { item.Id, item.CurrentStep })
            .ToListAsync(cancellationToken);

        var projectIds = projects.Select(item => item.Id).ToList();
        var steps = projectIds.Count == 0
            ? []
            : await db.SimulationSteps
                .Where(item => projectIds.Contains(item.SimulationProjectId))
                .OrderBy(item => item.SimulationProjectId)
                .ThenBy(item => item.StepNo)
                .ToListAsync(cancellationToken);

        var stepsByProjectId = steps
            .GroupBy(item => item.SimulationProjectId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var timelines = projects
            .Select(project => KnowledgeAnalysisService.BuildTimeline(stepsByProjectId.TryGetValue(project.Id, out var stepsForProject) ? stepsForProject : []))
            .Where(timeline => timeline.Count > 0)
            .ToList();

        var finalKnowledgePoints = timelines
            .Select(timeline => timeline.OrderBy(item => item.StepNo).Last())
            .ToList();

        var perRunSerendipityRates = timelines
            .Select(timeline => timeline.Count == 0 ? 0 : timeline.Count(item => item.SerendipityOccurred) / (double)timeline.Count)
            .ToList();

        var dominantBottleneck = finalKnowledgePoints
            .Select(item => string.IsNullOrWhiteSpace(item.PipelineBottleneck) ? "--" : item.PipelineBottleneck)
            .GroupBy(item => item)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.Key)
            .FirstOrDefault() ?? "--";

        var phaseSummary = BuildPhaseSummary(experimentRuns);
        var totalRunCount = Math.Max(1, experimentRuns.Count);

        return new PhaseDiagramPoint
        {
            PhaseDiagramId = phaseDiagramId,
            XValue = Math.Round(xValue, 3),
            YValue = Math.Round(yValue, 3),
            ExperimentId = experimentId,
            FinalPhaseSummary = JsonSerializer.Serialize(phaseSummary),
            DominantPhase = DetermineDominantPhase(phaseSummary),
            EmergentRate = Math.Round(GetPhaseRate(phaseSummary, SimulationPhase.Emergent, totalRunCount), 4),
            StableRate = Math.Round(GetPhaseRate(phaseSummary, SimulationPhase.Stable, totalRunCount), 4),
            LearningRate = Math.Round(GetPhaseRate(phaseSummary, SimulationPhase.Learning, totalRunCount), 4),
            SiloRate = Math.Round(GetPhaseRate(phaseSummary, SimulationPhase.Silo, totalRunCount), 4),
            ChaosRate = Math.Round(GetPhaseRate(phaseSummary, SimulationPhase.Chaos, totalRunCount), 4),
            CollapseRate = Math.Round(GetPhaseRate(phaseSummary, SimulationPhase.Collapse, totalRunCount), 4),
            AverageTrust = experimentRuns.Count == 0 ? 0 : Math.Round(experimentRuns.Average(item => item.AverageTrust), 4),
            AverageEffectiveDensity = experimentRuns.Count == 0 ? 0 : Math.Round(experimentRuns.Average(item => item.EffectiveNetworkDensity), 4),
            AverageKnowledgeDiversity = finalKnowledgePoints.Count == 0 ? 0 : Math.Round(finalKnowledgePoints.Average(item => item.KnowledgeDiversity), 4),
            AverageKnowledgeRecombinationScore = finalKnowledgePoints.Count == 0 ? 0 : Math.Round(finalKnowledgePoints.Average(item => item.KnowledgeRecombinationScore), 4),
            AverageKnowledgeReconfigurationScore = finalKnowledgePoints.Count == 0 ? 0 : Math.Round(finalKnowledgePoints.Average(item => item.KnowledgeReconfigurationScore), 4),
            AverageSerendipityRate = perRunSerendipityRates.Count == 0 ? 0 : Math.Round(perRunSerendipityRates.Average(), 4),
            AveragePipelineCompletionScore = finalKnowledgePoints.Count == 0 ? 0 : Math.Round(finalKnowledgePoints.Average(item => item.PipelineCompletionScore), 4),
            DominantBottleneck = dominantBottleneck,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static string DetermineDominantPhase(IReadOnlyDictionary<string, int> summary)
    {
        return summary
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => item.Key)
            .FirstOrDefault() ?? SimulationPhase.Forming;
    }

    private static double GetPhaseRate(IReadOnlyDictionary<string, int> summary, string phase, int totalRunCount)
    {
        if (totalRunCount <= 0)
        {
            return 0;
        }

        return summary.TryGetValue(phase, out var count) ? count / (double)totalRunCount : 0;
    }

    private static Dictionary<string, int> BuildPhaseSummary(IEnumerable<ExperimentRun> runs)
    {
        var phases = new[]
        {
            SimulationPhase.Forming,
            SimulationPhase.Learning,
            SimulationPhase.Stable,
            SimulationPhase.Adaptation,
            SimulationPhase.Emergent,
            SimulationPhase.Silo,
            SimulationPhase.Chaos,
            SimulationPhase.Collapse
        };

        return phases.ToDictionary(
            phase => phase,
            phase => runs.Count(run => string.Equals(run.FinalPhase, phase, StringComparison.OrdinalIgnoreCase)));
    }

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

    private static int CalculatePointCount(double startValue, double endValue, double stepValue)
    {
        if (stepValue <= 0)
        {
            return 0;
        }

        var start = decimal.Round((decimal)startValue, 3);
        var end = decimal.Round((decimal)endValue, 3);
        var step = decimal.Round((decimal)stepValue, 3);
        var count = 0;

        for (var value = start; value <= end + 0.0001m; value += step)
        {
            count++;
        }

        return count;
    }

    private static string FormatPhaseLabel(string? phase) => phase switch
    {
        SimulationPhase.Forming => "Forming",
        SimulationPhase.Learning => "Learning",
        SimulationPhase.Stable => "Stable",
        SimulationPhase.Adaptation => "Adaptation",
        SimulationPhase.Emergent => "Emergent",
        SimulationPhase.Silo => "Silo",
        SimulationPhase.Chaos => "Chaos",
        SimulationPhase.Collapse => "Collapse",
        _ => string.IsNullOrWhiteSpace(phase) ? "--" : phase.Trim()
    };

    private static string GetPointKey(double xValue, double yValue)
        => $"{Math.Round(xValue, 3).ToString("0.000", CultureInfo.InvariantCulture)}|{Math.Round(yValue, 3).ToString("0.000", CultureInfo.InvariantCulture)}";

    private static bool AreSamePhase(string? left, string? right)
        => string.Equals(NormalizePhase(left), NormalizePhase(right), StringComparison.OrdinalIgnoreCase);

    private static string NormalizePhase(string? phase)
        => string.IsNullOrWhiteSpace(phase) ? "--" : phase.Trim();

    public sealed record AdaptivePlan(
        string Name,
        string Description,
        string AdaptiveSourceType,
        string AdaptiveReason,
        double XStartValue,
        double XEndValue,
        double XStepValue,
        double YStartValue,
        double YEndValue,
        double YStepValue);
}
