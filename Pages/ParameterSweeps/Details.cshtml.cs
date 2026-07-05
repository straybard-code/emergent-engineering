using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class DetailsModel(AppDbContext db, ParameterSweepRunner sweepRunner) : PageModel
{
    [TempData]
    public string? SweepMessage { get; set; }

    public ParameterSweep? Sweep { get; private set; }
    public string ScenarioName { get; private set; } = "-";
    public List<ParameterSweepRun> Runs { get; private set; } = [];
    public List<ParameterSweepPhaseRow> PhaseComparison { get; private set; } = [];
    public List<ParameterSweepAnalysisPoint> AnalysisPoints { get; private set; } = [];
    public List<ImpactPathNode> ImpactPathNodes { get; private set; } = [];
    public List<SensitivityRankingRow> SensitivityRanking { get; private set; } = [];
    public Dictionary<int, string> ExperimentNames { get; private set; } = [];
    public string MaxAverageTrustSummary { get; private set; } = "--";
    public string MaxEffectiveDensitySummary { get; private set; } = "--";
    public string MaxEmergentRateSummary { get; private set; } = "--";
    public string MaxSerendipityRateSummary { get; private set; } = "--";
    public string MaxTrustJumpSummary { get; private set; } = "--";
    public string MaxEmergenceJumpSummary { get; private set; } = "--";
    public string ImpactPathInterpretation { get; private set; } = "--";
    public string ImpactPathBottleneck { get; private set; } = "--";
    public string SensitivityConclusion { get; private set; } = "--";
    public string TargetParameterLabel { get; private set; } = "-";
    public List<ParameterSweepSummaryCard> SummaryCards { get; private set; } = [];
    public List<PipelineStageSummaryRow> PipelineStageSummaries { get; private set; } = [];
    public string RecommendedInterpretation { get; private set; } = "--";
    public string PrimaryConclusionMessage { get; private set; } = "--";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
        if (Sweep is null)
        {
            return RedirectToPage("/ParameterSweeps/Index");
        }

        if (Sweep.Status == ParameterSweepStatus.Running)
        {
            try
            {
                await sweepRunner.RecalculateSweepStatusAsync(Sweep.Id);
            }
            catch
            {
                // Keep the stored status if recalculation fails.
            }
            Sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
            if (Sweep is null)
            {
                return RedirectToPage("/ParameterSweeps/Index");
            }
        }

        ScenarioName = await db.Scenarios
            .Where(item => item.Id == Sweep.ScenarioId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync() ?? "-";

        Runs = await db.ParameterSweepRuns
            .Where(item => item.ParameterSweepId == id)
            .OrderBy(item => item.ParameterValue)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync();

        var experimentIds = Runs
            .Select(item => item.ExperimentId)
            .Distinct()
            .ToList();

        ExperimentNames = experimentIds.Count == 0
            ? []
            : await db.Experiments
                .Where(item => experimentIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name);

        PhaseComparison = Runs
            .Select(item => new ParameterSweepPhaseRow
            {
                ParameterValue = item.ParameterValue,
                Counts = DeserializePhaseSummary(item.FinalPhaseSummaryJson)
            })
            .ToList();

        AnalysisPoints = await BuildAnalysisPointsAsync();
        BuildSweepSummaries();
        BuildImpactPathAnalysis();
        BuildSensitivityRanking();
        BuildDashboardSummary();

        return Page();
    }

    public async Task<IActionResult> OnPostStopAsync(int id)
    {
        var sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
        if (sweep is null)
        {
            return RedirectToPage("/ParameterSweeps/Index");
        }

        if (sweep.Status == ParameterSweepStatus.Running)
        {
            sweep.Status = ParameterSweepStatus.StopRequested;
            await db.SaveChangesAsync();
            SweepMessage = "停止要求を受け付けました。現在実行中のパラメータ値が終わった後に停止します。";
        }

        return RedirectToPage(new { id });
    }

    public string FormatStatus(string? status) => status switch
    {
        "Created" => "Created（作成済み）",
        "Running" => "Running（実行中）",
        "Completed" => "Completed（完了）",
        "Failed" => "Failed（失敗）",
        "StopRequested" => "StopRequested（停止要求）",
        "Stopped" => "Stopped（停止済み）",
        _ => status ?? "-"
    };

    public string FormatPhaseSummary(string json)
    {
        var summary = DeserializePhaseSummary(json);
        var formatted = string.Join(", ",
            summary
                .Where(item => item.Value > 0)
                .Select(item => $"{item.Key}: {item.Value}"));
        return string.IsNullOrWhiteSpace(formatted) ? "-" : formatted;
    }

    public string GetTrustChartJson()
    {
        var points = Runs.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            averageAbsTrust = item.AverageAbsTrust
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetExperimentName(int experimentId)
    {
        return ExperimentNames.TryGetValue(experimentId, out var name) ? name : $"Experiment #{experimentId}";
    }

    public string GetEmergentChartJson()
    {
        var points = PhaseComparison.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            emergentCount = item.GetCount(SimulationPhase.Emergent)
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetTrustDensityChartJson()
    {
        var points = AnalysisPoints.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            averageTrust = item.AverageTrust,
            effectiveDensity = item.EffectiveDensity
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetLinkChartJson()
    {
        var points = AnalysisPoints.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            strongLinks = item.StrongLinks,
            weakLinks = item.WeakLinks
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetPhaseRateChartJson()
    {
        var points = AnalysisPoints.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            emergentRate = item.EmergentRate,
            stableRate = item.StableRate,
            learningRate = item.LearningRate,
            siloRate = item.SiloRate
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetSerendipityChartJson()
    {
        var points = AnalysisPoints.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            serendipityRate = item.SerendipityRate,
            serendipityToEmergenceRate = item.SerendipityToEmergenceRate
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetKnowledgeChartJson()
    {
        var points = AnalysisPoints.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            averageKnowledgeDiversity = item.AverageKnowledgeDiversity,
            averageKnowledgeRecombinationScore = item.AverageKnowledgeRecombinationScore,
            averageKnowledgeReconfigurationScore = item.AverageKnowledgeReconfigurationScore
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetPhaseScoreChartJson()
    {
        var points = AnalysisPoints.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            averageEmergentScore = item.AverageEmergentScore,
            averageStableScore = item.AverageStableScore,
            averageLearningScore = item.AverageLearningScore,
            averageSiloScore = item.AverageSiloScore,
            averageAdaptationScore = item.AverageAdaptationScore
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetPipelineChartJson()
    {
        var points = AnalysisPoints.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            averageSerendipityScore = item.AverageSerendipityScore,
            averageKnowledgeRecombinationScore = item.AverageKnowledgeRecombinationScore,
            averageKnowledgeReconfigurationScore = item.AverageKnowledgeReconfigurationScore,
            averageLearningScore = item.AverageLearningScore,
            averageAdaptationScore = item.AverageAdaptationScore,
            averageEmergentScore = item.AverageEmergentScore
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetPipelineCompletionChartJson()
    {
        var points = AnalysisPoints.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            averagePipelineCompletionScore = item.AveragePipelineCompletionScore
        });

        return JsonSerializer.Serialize(points);
    }

    public string FormatDelta(double? value)
    {
        if (!value.HasValue)
        {
            return "--";
        }

        return value.Value >= 0 ? $"+{value.Value:0.000}" : value.Value.ToString("0.000");
    }

    public string GetTransitionRowClass(ParameterSweepAnalysisPoint point)
    {
        if (point.TransitionCandidates.Contains("創発ジャンプ", StringComparison.Ordinal))
        {
            return "threshold-sweep-critical";
        }

        return string.IsNullOrWhiteSpace(point.TransitionCandidates) ? string.Empty : "threshold-sweep-warning";
    }

    private async Task<List<ParameterSweepAnalysisPoint>> BuildAnalysisPointsAsync()
    {
        if (Runs.Count == 0)
        {
            return [];
        }

        var experimentIds = Runs
            .Select(item => item.ExperimentId)
            .Distinct()
            .ToList();

        var experimentRuns = await db.ExperimentRuns
            .Where(item => experimentIds.Contains(item.ExperimentId))
            .OrderBy(item => item.ExperimentId)
            .ThenBy(item => item.RunNo)
            .ToListAsync();

        var projects = await db.SimulationProjects
            .Include(item => item.Agents)
            .Include(item => item.Metrics)
            .Where(item => item.ExperimentId.HasValue && experimentIds.Contains(item.ExperimentId.Value))
            .OrderBy(item => item.ExperimentId)
            .ThenBy(item => item.Id)
            .ToListAsync();

        var projectIds = projects.Select(item => item.Id).ToList();
        var actions = await db.AgentActions
            .Where(item => projectIds.Contains(item.SimulationProjectId))
            .OrderBy(item => item.SimulationProjectId)
            .ThenBy(item => item.StepNo)
            .ThenBy(item => item.Id)
            .ToListAsync();
        var steps = await db.SimulationSteps
            .Where(item => projectIds.Contains(item.SimulationProjectId))
            .OrderBy(item => item.SimulationProjectId)
            .ThenBy(item => item.StepNo)
            .ToListAsync();

        var runsByExperimentId = experimentRuns
            .GroupBy(item => item.ExperimentId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var projectsByExperimentId = projects
            .Where(item => item.ExperimentId.HasValue)
            .GroupBy(item => item.ExperimentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());
        var actionsByProjectId = actions
            .GroupBy(item => item.SimulationProjectId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var stepsByProjectId = steps
            .GroupBy(item => item.SimulationProjectId)
            .ToDictionary(group => group.Key, group => group.ToList());

        List<ParameterSweepAnalysisPoint> points = [];
        foreach (var sweepRun in Runs)
        {
            var experimentProjects = projectsByExperimentId.GetValueOrDefault(sweepRun.ExperimentId, []);
            var projectRuns = runsByExperimentId.GetValueOrDefault(sweepRun.ExperimentId, []);
            var analyses = experimentProjects
                .Select(project => AnalyzeProject(
                    project,
                    stepsByProjectId.GetValueOrDefault(project.Id, []),
                    actionsByProjectId.GetValueOrDefault(project.Id, [])))
                .ToList();

            points.Add(BuildAnalysisPoint(sweepRun.ParameterValue, analyses, projectRuns));
        }

        ApplyDeltas(points);
        return points;
    }

    private static ParameterSweepAnalysisPoint BuildAnalysisPoint(
        double parameterValue,
        IReadOnlyCollection<ProjectAnalysisResult> analyses,
        IReadOnlyCollection<ExperimentRun> runs)
    {
        var totalRunCount = Math.Max(analyses.Count, runs.Count);
        var emergentRunCount = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase));
        var stableRunCount = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase));
        var learningRunCount = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase));
        var siloRunCount = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Silo, StringComparison.OrdinalIgnoreCase));
        var trustInsufficientRunCount = 0;
        var effectiveDensityInsufficientRunCount = 0;
        var strongLinkInsufficientRunCount = 0;
        var knowledgeDiversityInsufficientRunCount = 0;
        var knowledgeRecombinationInsufficientRunCount = 0;
        var knowledgeReconfigurationInsufficientRunCount = 0;
        var serendipityInsufficientRunCount = 0;
        var ideaProposalInsufficientRunCount = 0;
        var shareInfoInsufficientRunCount = 0;
        var psychologicalSafetyInsufficientRunCount = 0;
        var constructiveCriticismInsufficientRunCount = 0;
        List<string> failureReasons = [];
        List<string> pipelineBottlenecks = [];

        foreach (var analysis in analyses)
        {
            if (!string.IsNullOrWhiteSpace(analysis.MostCommonPipelineBottleneck)
                && analysis.MostCommonPipelineBottleneck != "--")
            {
                pipelineBottlenecks.Add(analysis.MostCommonPipelineBottleneck);
            }

            var reason = DetermineEmergentFailureReason(
                analysis,
                out var trustInsufficient,
                out var densityInsufficient,
                out var strongLinkInsufficient,
                out var knowledgeDiversityInsufficient,
                out var knowledgeRecombinationInsufficient,
                out var knowledgeReconfigurationInsufficient,
                out var serendipityInsufficient,
                out var ideaProposalInsufficient,
                out var shareInfoInsufficient,
                out var psychologicalSafetyInsufficient,
                out var constructiveCriticismInsufficient);

            if (!string.IsNullOrWhiteSpace(reason))
            {
                failureReasons.Add(reason);
            }

            trustInsufficientRunCount += trustInsufficient ? 1 : 0;
            effectiveDensityInsufficientRunCount += densityInsufficient ? 1 : 0;
            strongLinkInsufficientRunCount += strongLinkInsufficient ? 1 : 0;
            knowledgeDiversityInsufficientRunCount += knowledgeDiversityInsufficient ? 1 : 0;
            knowledgeRecombinationInsufficientRunCount += knowledgeRecombinationInsufficient ? 1 : 0;
            knowledgeReconfigurationInsufficientRunCount += knowledgeReconfigurationInsufficient ? 1 : 0;
            serendipityInsufficientRunCount += serendipityInsufficient ? 1 : 0;
            ideaProposalInsufficientRunCount += ideaProposalInsufficient ? 1 : 0;
            shareInfoInsufficientRunCount += shareInfoInsufficient ? 1 : 0;
            psychologicalSafetyInsufficientRunCount += psychologicalSafetyInsufficient ? 1 : 0;
            constructiveCriticismInsufficientRunCount += constructiveCriticismInsufficient ? 1 : 0;
        }

        var mainFailureReason = failureReasons.Count == 0
            ? "--"
            : failureReasons
                .GroupBy(item => item)
                .Select(group => new { Reason = group.Key, Count = group.Count() })
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Reason)
                .First()
                .Reason;
        var mostCommonPipelineBottleneck = BuildMostCommonPipelineBottleneck(pipelineBottlenecks);

        return new ParameterSweepAnalysisPoint
        {
            ParameterValue = parameterValue,
            AverageTrust = RoundAverage(analyses.Select(item => item.Metrics.AverageTrust)),
            AverageAbsTrust = RoundAverage(analyses.Select(item => item.Metrics.AverageAbsTrust)),
            EffectiveDensity = RoundAverage(analyses.Select(item => item.Metrics.EffectiveNetworkDensity)),
            StrongLinks = RoundAverage(analyses.Select(item => (double)item.Metrics.StrongLinkCount)),
            WeakLinks = RoundAverage(analyses.Select(item => (double)item.Metrics.WeakLinkCount)),
            ComponentCount = RoundAverage(analyses.Select(item => (double)item.Metrics.ComponentCount)),
            IsolatedAgents = RoundAverage(analyses.Select(item => (double)item.Metrics.IsolatedCount)),
            AverageKnowledgeDiversity = RoundAverage(analyses.Select(item => item.AverageKnowledgeDiversity)),
            AverageKnowledgeRecombinationScore = RoundAverage(analyses.Select(item => item.AverageKnowledgeRecombinationScore)),
            AverageKnowledgeReconfigurationScore = RoundAverage(analyses.Select(item => item.AverageKnowledgeReconfigurationScore)),
            AverageEmergentScore = RoundAverage(analyses.Select(item => item.FinalKnowledgePoint?.EmergentScore ?? 0)),
            AverageStableScore = RoundAverage(analyses.Select(item => item.FinalKnowledgePoint?.StableScore ?? 0)),
            AverageLearningScore = RoundAverage(analyses.Select(item => item.FinalKnowledgePoint?.LearningScore ?? 0)),
            AverageSiloScore = RoundAverage(analyses.Select(item => item.FinalKnowledgePoint?.SiloScore ?? 0)),
            AverageAdaptationScore = RoundAverage(analyses.Select(item => item.FinalKnowledgePoint?.AdaptationScore ?? 0)),
            AveragePipelineCompletionScore = RoundAverage(analyses.Select(item => item.AveragePipelineCompletionScore)),
            MostCommonPipelineBottleneck = mostCommonPipelineBottleneck,
            PipelineBottleneckInterpretation = KnowledgeAnalysisService.BuildPipelineBottleneckInterpretation(mostCommonPipelineBottleneck),
            EmergentRunCount = emergentRunCount,
            StableRunCount = stableRunCount,
            LearningRunCount = learningRunCount,
            SiloRunCount = siloRunCount,
            TotalRunCount = totalRunCount,
            EmergentRate = totalRunCount == 0 ? 0 : Math.Round(emergentRunCount / (double)totalRunCount, 3),
            StableRate = totalRunCount == 0 ? 0 : Math.Round(stableRunCount / (double)totalRunCount, 3),
            LearningRate = totalRunCount == 0 ? 0 : Math.Round(learningRunCount / (double)totalRunCount, 3),
            SiloRate = totalRunCount == 0 ? 0 : Math.Round(siloRunCount / (double)totalRunCount, 3),
            AverageSerendipityScore = RoundAverage(analyses.Select(item => item.AverageSerendipityScore)),
            SerendipityOccurredRunCount = analyses.Count(item => item.SerendipityOccurred),
            SerendipityRate = totalRunCount == 0 ? 0 : Math.Round(analyses.Count(item => item.SerendipityOccurred) / (double)totalRunCount, 3),
            SerendipityToEmergenceLinkCount = analyses.Count(item => item.SerendipityToEmergenceLink),
            SerendipityToEmergenceRate = totalRunCount == 0 ? 0 : Math.Round(analyses.Count(item => item.SerendipityToEmergenceLink) / (double)totalRunCount, 3),
            TrustInsufficientRunCount = trustInsufficientRunCount,
            EffectiveDensityInsufficientRunCount = effectiveDensityInsufficientRunCount,
            StrongLinkInsufficientRunCount = strongLinkInsufficientRunCount,
            KnowledgeDiversityInsufficientRunCount = knowledgeDiversityInsufficientRunCount,
            KnowledgeRecombinationInsufficientRunCount = knowledgeRecombinationInsufficientRunCount,
            KnowledgeReconfigurationInsufficientRunCount = knowledgeReconfigurationInsufficientRunCount,
            SerendipityInsufficientRunCount = serendipityInsufficientRunCount,
            IdeaProposalInsufficientRunCount = ideaProposalInsufficientRunCount,
            ShareInfoInsufficientRunCount = shareInfoInsufficientRunCount,
            PsychologicalSafetyInsufficientRunCount = psychologicalSafetyInsufficientRunCount,
            ConstructiveCriticismInsufficientRunCount = constructiveCriticismInsufficientRunCount,
            MainEmergentFailureReason = mainFailureReason
        };
    }

    private static ProjectAnalysisResult AnalyzeProject(
        SimulationProject project,
        IReadOnlyCollection<SimulationStep> steps,
        IReadOnlyCollection<AgentAction> actions)
    {
        var metrics = NetworkMetricsCalculator.CalculateFromAgents(project.Agents, project.EffectiveTrustThreshold);
        var knowledgeTimeline = KnowledgeAnalysisService.BuildTimeline(steps);
        var actionDistribution = ActionDistributionCalculator.Calculate(actions.Select(action => action.Action));
        var averageSerendipityScore = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.SerendipityScore), 3);
        var averageKnowledgeDiversity = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.KnowledgeDiversity), 3);
        var averageKnowledgeRecombinationScore = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.KnowledgeRecombinationScore), 3);
        var averageKnowledgeReconfigurationScore = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.KnowledgeReconfigurationScore), 3);
        var averageLearningScore = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.LearningScore), 3);
        var averageAdaptationScore = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.AdaptationScore), 3);
        var averageEmergentScore = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.EmergentScore), 3);
        var averagePipelineCompletionScore = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.PipelineCompletionScore), 3);
        var hasPipelineData = knowledgeTimeline.Any(item =>
            item.SerendipityScore > 0
            || item.KnowledgeRecombinationScore > 0
            || item.KnowledgeReconfigurationScore > 0
            || item.LearningScore > 0
            || item.AdaptationScore > 0
            || item.EmergentScore > 0
            || item.PipelineCompletionScore > 0);
        var mostCommonPipelineBottleneck = !hasPipelineData
            ? "--"
            : BuildMostCommonPipelineBottleneck(
                knowledgeTimeline
                    .Select(item => item.PipelineBottleneck)
                    .Where(item => !string.IsNullOrWhiteSpace(item) && item != "--")
                    .ToList());

        return new ProjectAnalysisResult
        {
            Metrics = metrics,
            AverageSerendipityScore = averageSerendipityScore,
            AverageKnowledgeDiversity = averageKnowledgeDiversity,
            AverageKnowledgeRecombinationScore = averageKnowledgeRecombinationScore,
            AverageKnowledgeReconfigurationScore = averageKnowledgeReconfigurationScore,
            AverageLearningScore = averageLearningScore,
            AverageAdaptationScore = averageAdaptationScore,
            AverageEmergentScore = averageEmergentScore,
            AveragePipelineCompletionScore = averagePipelineCompletionScore,
            MostCommonPipelineBottleneck = mostCommonPipelineBottleneck,
            SerendipityOccurred = knowledgeTimeline.Any(item => item.SerendipityOccurred),
            SerendipityToEmergenceLink = knowledgeTimeline.Any(item => item.SerendipityToEmergenceLink),
            FinalKnowledgePoint = knowledgeTimeline.OrderBy(item => item.StepNo).LastOrDefault(),
            ActionDistribution = actionDistribution,
            AgentCount = project.Agents.Count,
            PsychologicalSafetyLevel = project.PsychologicalSafetyLevel
        };
    }

    private static void ApplyDeltas(IReadOnlyList<ParameterSweepAnalysisPoint> points)
    {
        for (var index = 0; index < points.Count; index++)
        {
            var current = points[index];
            if (index == 0)
            {
                current.DeltaAverageTrust = null;
                current.DeltaEffectiveDensity = null;
                current.DeltaEmergentRate = null;
                current.DeltaSerendipityRate = null;
                current.DeltaKnowledgeDiversity = null;
                current.DeltaKnowledgeRecombinationScore = null;
                current.DeltaKnowledgeReconfigurationScore = null;
                current.DeltaEmergentScore = null;
                current.DeltaStableScore = null;
                current.DeltaLearningScore = null;
                current.DeltaPipelineCompletionScore = null;
                current.PreviousParameterValue = null;
                current.TransitionCandidates = "";
                continue;
            }

            var previous = points[index - 1];
            current.PreviousParameterValue = previous.ParameterValue;
            current.DeltaAverageTrust = Math.Round(current.AverageTrust - previous.AverageTrust, 3);
            current.DeltaEffectiveDensity = Math.Round(current.EffectiveDensity - previous.EffectiveDensity, 3);
            current.DeltaEmergentRate = Math.Round(current.EmergentRate - previous.EmergentRate, 3);
            current.DeltaSerendipityRate = Math.Round(current.SerendipityRate - previous.SerendipityRate, 3);
            current.DeltaKnowledgeDiversity = Math.Round(current.AverageKnowledgeDiversity - previous.AverageKnowledgeDiversity, 3);
            current.DeltaKnowledgeRecombinationScore = Math.Round(current.AverageKnowledgeRecombinationScore - previous.AverageKnowledgeRecombinationScore, 3);
            current.DeltaKnowledgeReconfigurationScore = Math.Round(current.AverageKnowledgeReconfigurationScore - previous.AverageKnowledgeReconfigurationScore, 3);
            current.DeltaEmergentScore = Math.Round(current.AverageEmergentScore - previous.AverageEmergentScore, 3);
            current.DeltaStableScore = Math.Round(current.AverageStableScore - previous.AverageStableScore, 3);
            current.DeltaLearningScore = Math.Round(current.AverageLearningScore - previous.AverageLearningScore, 3);
            current.DeltaPipelineCompletionScore = Math.Round(current.AveragePipelineCompletionScore - previous.AveragePipelineCompletionScore, 3);

            List<string> candidates = [];
            if (current.DeltaAverageTrust >= 0.15)
            {
                candidates.Add("信頼ジャンプ");
            }

            if (current.DeltaEffectiveDensity >= 0.15)
            {
                candidates.Add("密度ジャンプ");
            }

            if (current.DeltaEmergentRate >= 0.20)
            {
                candidates.Add("創発ジャンプ");
            }

            current.TransitionCandidates = string.Join("、", candidates);
        }
    }

    private void BuildSweepSummaries()
    {
        MaxAverageTrustSummary = BuildMaxMetricSummary(AnalysisPoints, item => item.AverageTrust, "AverageTrust");
        MaxEffectiveDensitySummary = BuildMaxMetricSummary(AnalysisPoints, item => item.EffectiveDensity, "EffectiveDensity");
        MaxEmergentRateSummary = BuildMaxMetricSummary(AnalysisPoints, item => item.EmergentRate, "EmergentRate");
        MaxSerendipityRateSummary = BuildMaxMetricSummary(AnalysisPoints, item => item.SerendipityRate, "SerendipityRate");
        MaxTrustJumpSummary = BuildMaxDeltaSummary(AnalysisPoints, item => item.DeltaAverageTrust);
        MaxEmergenceJumpSummary = BuildMaxDeltaSummary(AnalysisPoints, item => item.DeltaEmergentRate);
    }

    private void BuildImpactPathAnalysis()
    {
        TargetParameterLabel = Sweep is null
            ? "-"
            : EmergentEngineering.Models.BoundaryParameterNames.GetLabel(Sweep.TargetParameter);

        if (AnalysisPoints.Count == 0)
        {
            ImpactPathNodes = [];
            ImpactPathInterpretation = "--";
            ImpactPathBottleneck = "--";
            return;
        }

        var ordered = AnalysisPoints
            .OrderBy(item => item.ParameterValue)
            .ToList();
        var minPoint = ordered.First();
        var maxPoint = ordered.Last();

        var deltaAverageTrust = Math.Round(maxPoint.AverageTrust - minPoint.AverageTrust, 3);
        var deltaEffectiveDensity = Math.Round(maxPoint.EffectiveDensity - minPoint.EffectiveDensity, 3);
        var deltaStrongLinks = Math.Round(maxPoint.StrongLinks - minPoint.StrongLinks, 3);
        var deltaKnowledgeDiversity = Math.Round(maxPoint.AverageKnowledgeDiversity - minPoint.AverageKnowledgeDiversity, 3);
        var deltaKnowledgeRecombination = Math.Round(maxPoint.AverageKnowledgeRecombinationScore - minPoint.AverageKnowledgeRecombinationScore, 3);
        var deltaKnowledgeReconfiguration = Math.Round(maxPoint.AverageKnowledgeReconfigurationScore - minPoint.AverageKnowledgeReconfigurationScore, 3);
        var deltaSerendipityRate = Math.Round(maxPoint.SerendipityRate - minPoint.SerendipityRate, 3);
        var deltaSerendipityToEmergenceRate = Math.Round(maxPoint.SerendipityToEmergenceRate - minPoint.SerendipityToEmergenceRate, 3);
        var deltaEmergentScore = Math.Round(maxPoint.AverageEmergentScore - minPoint.AverageEmergentScore, 3);
        var deltaEmergentRate = Math.Round(maxPoint.EmergentRate - minPoint.EmergentRate, 3);

        ImpactPathNodes =
        [
            new ImpactPathNode
            {
                Title = "対象パラメータ",
                ValueLine = TargetParameterLabel,
                NoteLine = $"{minPoint.ParameterValue:0.000} → {maxPoint.ParameterValue:0.000}",
                CssClass = "impact-node-parameter"
            },
            BuildImpactNode("平均信頼度", deltaAverageTrust, false),
            BuildImpactNode("実効密度", deltaEffectiveDensity, false),
            BuildImpactNode("Strong Links", deltaStrongLinks, true),
            BuildImpactNode("知識多様性", deltaKnowledgeDiversity, false),
            BuildImpactNode("知識再結合", deltaKnowledgeRecombination, false),
            BuildImpactNode("知識再構成", deltaKnowledgeReconfiguration, false),
            BuildImpactNode("セレンディピティ率", deltaSerendipityRate, false),
            BuildImpactNode("セレンディピティ→創発率", deltaSerendipityToEmergenceRate, false),
            BuildImpactNode("EmergentScore", deltaEmergentScore, false),
            BuildImpactNode("EmergentRate", deltaEmergentRate, false)
        ];

        ImpactPathBottleneck = BuildImpactBottleneck(
            deltaAverageTrust,
            deltaEffectiveDensity,
            deltaKnowledgeRecombination,
            deltaSerendipityRate,
            deltaEmergentScore,
            deltaEmergentRate);
        ImpactPathInterpretation = BuildImpactInterpretation(
            deltaAverageTrust,
            deltaEffectiveDensity,
            deltaStrongLinks,
            deltaKnowledgeDiversity,
            deltaKnowledgeRecombination,
            deltaKnowledgeReconfiguration,
            deltaSerendipityRate,
            deltaSerendipityToEmergenceRate,
            deltaEmergentScore,
            deltaEmergentRate,
            ImpactPathBottleneck);
    }

    private void BuildSensitivityRanking()
    {
        if (AnalysisPoints.Count == 0)
        {
            SensitivityRanking = [];
            SensitivityConclusion = "--";
            return;
        }

        var ordered = AnalysisPoints
            .OrderBy(item => item.ParameterValue)
            .ToList();
        var minPoint = ordered.First();
        var maxPoint = ordered.Last();

        var rows = new List<SensitivityRankingRow>
        {
            BuildSensitivityRow("平均信頼度", minPoint.AverageTrust, maxPoint.AverageTrust, false),
            BuildSensitivityRow("実効密度", minPoint.EffectiveDensity, maxPoint.EffectiveDensity, false),
            BuildSensitivityRow("Strong Links", minPoint.StrongLinks, maxPoint.StrongLinks, true),
            BuildSensitivityRow("EmergentRate", minPoint.EmergentRate, maxPoint.EmergentRate, false),
            BuildSensitivityRow("StableRate", minPoint.StableRate, maxPoint.StableRate, false),
            BuildSensitivityRow("LearningRate", minPoint.LearningRate, maxPoint.LearningRate, false),
            BuildSensitivityRow("セレンディピティ率", minPoint.SerendipityRate, maxPoint.SerendipityRate, false),
            BuildSensitivityRow("セレンディピティ→創発率", minPoint.SerendipityToEmergenceRate, maxPoint.SerendipityToEmergenceRate, false),
            BuildSensitivityRow("知識多様性", minPoint.AverageKnowledgeDiversity, maxPoint.AverageKnowledgeDiversity, false),
            BuildSensitivityRow("知識再結合", minPoint.AverageKnowledgeRecombinationScore, maxPoint.AverageKnowledgeRecombinationScore, false),
            BuildSensitivityRow("知識再構成", minPoint.AverageKnowledgeReconfigurationScore, maxPoint.AverageKnowledgeReconfigurationScore, false),
            BuildSensitivityRow("AveragePipelineCompletionScore", minPoint.AveragePipelineCompletionScore, maxPoint.AveragePipelineCompletionScore, false),
            BuildSensitivityRow("AverageEmergentScore", minPoint.AverageEmergentScore, maxPoint.AverageEmergentScore, false),
            BuildSensitivityRow("AverageStableScore", minPoint.AverageStableScore, maxPoint.AverageStableScore, false),
            BuildSensitivityRow("AverageLearningScore", minPoint.AverageLearningScore, maxPoint.AverageLearningScore, false)
        };

        SensitivityRanking = rows
            .OrderByDescending(item => item.AbsoluteDelta)
            .ThenBy(item => item.Metric)
            .Select((item, index) => item with { Rank = index + 1 })
            .ToList();

        SensitivityConclusion = BuildSensitivityConclusion(SensitivityRanking);
    }

    private void BuildDashboardSummary()
    {
        if (AnalysisPoints.Count == 0)
        {
            SummaryCards = [];
            PipelineStageSummaries = [];
            RecommendedInterpretation = "このデータには創発パイプライン情報が含まれていません。新しくスイープを実行すると表示されます。";
            PrimaryConclusionMessage = "このデータには創発パイプライン情報が含まれていません。新しくスイープを実行すると表示されます。";
            return;
        }

        var mostCommonPhase = ResolveMostCommonFinalPhase();
        var emergentRateMax = AnalysisPoints.Max(item => item.EmergentRate);
        var pipelineCompletionMax = AnalysisPoints.Max(item => item.AveragePipelineCompletionScore);
        var mostCommonBottleneck = BuildMostCommonPipelineBottleneck(
            AnalysisPoints
                .Select(item => item.MostCommonPipelineBottleneck)
                .Where(item => !string.IsNullOrWhiteSpace(item) && item != "--")
                .ToList());
        var averageTrustMax = AnalysisPoints.Max(item => item.AverageTrust);
        var effectiveDensityMax = AnalysisPoints.Max(item => item.EffectiveDensity);
        var serendipityRateMax = AnalysisPoints.Max(item => item.SerendipityRate);
        var knowledgeReconfigurationMax = AnalysisPoints.Max(item => item.AverageKnowledgeReconfigurationScore);

        RecommendedInterpretation = BuildRecommendedInterpretation(
            emergentRateMax,
            pipelineCompletionMax,
            averageTrustMax,
            effectiveDensityMax,
            serendipityRateMax,
            knowledgeReconfigurationMax);
        PrimaryConclusionMessage = BuildPrimaryConclusionMessage(
            SensitivityRanking,
            emergentRateMax,
            pipelineCompletionMax);

        SummaryCards =
        [
            new ParameterSweepSummaryCard { Label = "最終相の最多", Value = FormatPhaseDisplay(mostCommonPhase) },
            new ParameterSweepSummaryCard { Label = "Emergent Rate 最大", Value = emergentRateMax.ToString("0.00") },
            new ParameterSweepSummaryCard { Label = "Pipeline Completion 最大", Value = pipelineCompletionMax.ToString("0.00") },
            new ParameterSweepSummaryCard { Label = "最頻ボトルネック", Value = string.IsNullOrWhiteSpace(mostCommonBottleneck) ? "--" : mostCommonBottleneck },
            new ParameterSweepSummaryCard { Label = "最大Average Trust", Value = averageTrustMax.ToString("0.00") },
            new ParameterSweepSummaryCard { Label = "最大Effective Density", Value = effectiveDensityMax.ToString("0.00") },
            new ParameterSweepSummaryCard { Label = "最大Serendipity Rate", Value = serendipityRateMax.ToString("0.00") },
            new ParameterSweepSummaryCard { Label = "最大Knowledge Reconfiguration", Value = knowledgeReconfigurationMax.ToString("0.00") }
        ];

        PipelineStageSummaries = BuildPipelineStageSummaries();
        if (PipelineStageSummaries.Count == 0)
        {
            RecommendedInterpretation = "このデータには創発パイプライン情報が含まれていません。新しくスイープを実行すると表示されます。";
            PrimaryConclusionMessage = "このデータには創発パイプライン情報が含まれていません。新しくスイープを実行すると表示されます。";
        }
    }

    private static ImpactPathNode BuildImpactNode(string title, double delta, bool strongLink)
    {
        return new ImpactPathNode
        {
            Title = title,
            ValueLine = FormatSignedDelta(delta, strongLink),
            NoteLine = GetImpactStrengthLabel(delta, strongLink),
            CssClass = GetImpactCssClass(delta, strongLink)
        };
    }

    private static SensitivityRankingRow BuildSensitivityRow(string metric, double minValue, double maxValue, bool strongLink)
    {
        var delta = Math.Round(maxValue - minValue, 3);
        return new SensitivityRankingRow
        {
            Metric = metric,
            MinValue = Math.Round(minValue, 3),
            MaxValue = Math.Round(maxValue, 3),
            Delta = delta,
            AbsoluteDelta = Math.Abs(delta),
            ImpactLabel = GetImpactStrengthLabel(delta, strongLink),
            CssClass = GetImpactCssClass(delta, strongLink),
            Interpretation = BuildSensitivityInterpretation(metric, delta)
        };
    }

    private static string GetImpactCssClass(double delta, bool strongLink)
    {
        var magnitude = Math.Abs(delta);
        if (magnitude >= (strongLink ? 50 : 0.20))
        {
            return "impact-node-strong";
        }

        if (magnitude >= (strongLink ? 10 : 0.05))
        {
            return "impact-node-medium";
        }

        return "impact-node-weak";
    }

    private static string GetImpactStrengthLabel(double delta, bool strongLink)
    {
        var magnitude = Math.Abs(delta);
        if (magnitude >= (strongLink ? 50 : 0.20))
        {
            return "強い影響";
        }

        if (magnitude >= (strongLink ? 10 : 0.05))
        {
            return "中程度の影響";
        }

        return "弱い影響";
    }

    private static string BuildSensitivityInterpretation(string metric, double delta)
    {
        if (Math.Abs(delta) < 0.01)
        {
            return "影響小";
        }

        return metric switch
        {
            "平均信頼度" => "信頼形成への影響が大きい",
            "実効密度" => "ネットワーク形成への影響が大きい",
            "Strong Links" => "強いリンク形成への影響が大きい",
            "セレンディピティ率" => "偶然の有用結合への影響が大きい",
            "知識再構成" => "知識構造の再編への影響が大きい",
            "AveragePipelineCompletionScore" => "創発プロセス全体への影響が大きい",
            "EmergentRate" => "創発相への到達に直接影響している",
            "AverageEmergentScore" => "創発判定スコアへの影響が大きい",
            _ => "中間指標への影響が確認できる"
        };
    }

    private static string FormatSignedDelta(double value, bool integerLike = false)
    {
        if (integerLike)
        {
            return value >= 0 ? $"+{value:0}" : value.ToString("0");
        }

        return value >= 0 ? $"+{value:0.000}" : value.ToString("0.000");
    }

    public string FormatImpactDelta(double value, bool integerLike = false)
    {
        return FormatSignedDelta(value, integerLike);
    }

    public static string FormatPhaseDisplay(string? phase) => phase switch
    {
        SimulationPhase.Forming => "Forming（形成期）",
        SimulationPhase.Learning => "Learning（学習期）",
        SimulationPhase.Stable => "Stable（安定期）",
        SimulationPhase.Emergent => "Emergent（創発期）",
        SimulationPhase.Silo => "Silo（サイロ化）",
        SimulationPhase.Chaos => "Chaos（混乱）",
        SimulationPhase.Collapse => "Collapse（崩壊）",
        SimulationPhase.Adaptation => "Adaptation（適応期）",
        _ => string.IsNullOrWhiteSpace(phase) ? "--" : phase
    };

    private static string BuildImpactBottleneck(
        double deltaAverageTrust,
        double deltaEffectiveDensity,
        double deltaKnowledgeRecombination,
        double deltaSerendipityRate,
        double deltaEmergentScore,
        double deltaEmergentRate)
    {
        if (deltaAverageTrust >= 0.10 && deltaEffectiveDensity < 0.05)
        {
            return "ボトルネック: 信頼からネットワークへの変換不足";
        }

        if (deltaEffectiveDensity >= 0.10 && deltaKnowledgeRecombination < 0.05)
        {
            return "ボトルネック: ネットワークから知識再結合への変換不足";
        }

        if (deltaKnowledgeRecombination >= 0.10 && deltaSerendipityRate < 0.05)
        {
            return "ボトルネック: 知識再結合からセレンディピティへの変換不足";
        }

        if (deltaSerendipityRate >= 0.10 && deltaEmergentScore < 0.05)
        {
            return "ボトルネック: セレンディピティから創発スコアへの変換不足";
        }

        if (deltaEmergentScore >= 0.10 && deltaEmergentRate < 0.05)
        {
            return "ボトルネック: 創発スコアから創発相判定への変換不足";
        }

        return "明確な停止点は見つかりませんでした。";
    }

    private static string BuildImpactInterpretation(
        double deltaAverageTrust,
        double deltaEffectiveDensity,
        double deltaStrongLinks,
        double deltaKnowledgeDiversity,
        double deltaKnowledgeRecombination,
        double deltaKnowledgeReconfiguration,
        double deltaSerendipityRate,
        double deltaSerendipityToEmergenceRate,
        double deltaEmergentScore,
        double deltaEmergentRate,
        string bottleneck)
    {
        if (bottleneck.Contains("信頼からネットワーク", StringComparison.Ordinal))
        {
            return "対象パラメータは平均信頼度を大きく変化させましたが、実効密度への伝播は弱く、創発相には届きませんでした。";
        }

        if (bottleneck.Contains("ネットワークから知識再結合", StringComparison.Ordinal))
        {
            return "信頼ネットワークは変化しましたが、知識再結合への伝播が弱く、後続の変化が止まっています。";
        }

        if (bottleneck.Contains("知識再結合からセレンディピティ", StringComparison.Ordinal))
        {
            return "知識再結合は増えていますが、偶然有用な結合としてのセレンディピティに十分つながっていません。";
        }

        if (bottleneck.Contains("セレンディピティから創発スコア", StringComparison.Ordinal))
        {
            return "セレンディピティは立ち上がっていますが、創発スコアへの接続が弱い状態です。";
        }

        if (bottleneck.Contains("創発スコアから創発相判定", StringComparison.Ordinal))
        {
            return "EmergentScore は上昇していますが、創発相判定の閾値または優先順位がボトルネックの可能性があります。";
        }

        if (deltaAverageTrust > 0 && deltaEffectiveDensity > 0 && deltaKnowledgeRecombination <= 0 && deltaSerendipityRate <= 0)
        {
            return "信頼とネットワークは変化しましたが、知識再結合やセレンディピティには十分伝わっていません。";
        }

        if (deltaKnowledgeRecombination > 0 && deltaSerendipityRate <= 0)
        {
            return "知識再結合は増加しましたが、SerendipityRate が増えていないため、偶然有用な結合として検出されていません。";
        }

        if (deltaEmergentScore > 0 && deltaEmergentRate <= 0)
        {
            return "EmergentScore は上昇していますが、EmergentRate が変化していないため、相判定または優先順位がボトルネックの可能性があります。";
        }

        if (deltaStrongLinks > 0 && deltaKnowledgeDiversity <= 0 && deltaKnowledgeReconfiguration <= 0)
        {
            return "ネットワークは強くなっていますが、知識側の変化にはつながっていません。";
        }

        return "対象パラメータの影響は複数指標に分散しており、明確な単一路線は見えていません。";
    }

    private static string BuildSensitivityConclusion(IReadOnlyList<SensitivityRankingRow> ranking)
    {
        if (ranking.Count == 0)
        {
            return "--";
        }

        var top = ranking.First();
        var emergentRateRow = ranking.FirstOrDefault(item => item.Metric == "EmergentRate");
        if (emergentRateRow is not null
            && (top.Metric == "EmergentRate" || emergentRateRow.AbsoluteDelta >= 0.20))
        {
            return "このパラメータは創発相への直接レバーである可能性があります。";
        }

        var emergentScore = ranking.FirstOrDefault(item => item.Metric == "AverageEmergentScore");
        if (emergentScore is not null && emergentScore.Delta > 0)
        {
            var emergentRate = ranking.FirstOrDefault(item => item.Metric == "EmergentRate");
            if (emergentRate is null || emergentRate.Delta <= 0)
            {
                return "このパラメータは創発準備状態を高めますが、相判定または他条件がボトルネックです。";
            }
        }

        var trustDelta = ranking.Any(item => (item.Metric == "平均信頼度" || item.Metric == "実効密度") && item.Delta > 0);
        var knowledgeDelta = ranking.Any(item => (item.Metric == "知識多様性" || item.Metric == "知識再結合" || item.Metric == "知識再構成" || item.Metric == "セレンディピティ率" || item.Metric == "セレンディピティ→創発率" || item.Metric == "AveragePipelineCompletionScore") && item.Delta > 0);
        if (trustDelta && !knowledgeDelta)
        {
            return "このパラメータは信頼ネットワーク形成には効きますが、知識再結合には届いていません。";
        }

        if (!trustDelta && knowledgeDelta)
        {
            return "知識側の変化はありますが、創発スコアへの接続が弱い可能性があります。";
        }

        if (ranking.All(item => item.AbsoluteDelta < 0.05))
        {
            return "この条件範囲では対象パラメータの感度は低いです。";
        }

        return "対象パラメータは複数の中間指標に反応しています。";
    }

    private string ResolveMostCommonFinalPhase()
    {
        var phase = Runs
            .SelectMany(item => DeserializePhaseSummary(item.FinalPhaseSummaryJson))
            .GroupBy(item => item.Key)
            .Select(group => new
            {
                Phase = group.Key,
                Count = group.Sum(item => item.Value)
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Phase, StringComparer.Ordinal)
            .FirstOrDefault();

        return phase is null || phase.Count == 0 ? "--" : phase.Phase;
    }

    private List<PipelineStageSummaryRow> BuildPipelineStageSummaries()
    {
        var rows = new List<PipelineStageSummaryRow>
        {
            new() { Label = "Serendipity", Value = RoundAverage(AnalysisPoints.Select(item => item.AverageSerendipityScore)) },
            new() { Label = "Knowledge Recombination", Value = RoundAverage(AnalysisPoints.Select(item => item.AverageKnowledgeRecombinationScore)) },
            new() { Label = "Knowledge Reconfiguration", Value = RoundAverage(AnalysisPoints.Select(item => item.AverageKnowledgeReconfigurationScore)) },
            new() { Label = "Learning", Value = RoundAverage(AnalysisPoints.Select(item => item.AverageLearningScore)) },
            new() { Label = "Adaptation", Value = RoundAverage(AnalysisPoints.Select(item => item.AverageAdaptationScore)) },
            new() { Label = "Emergence", Value = RoundAverage(AnalysisPoints.Select(item => item.AverageEmergentScore)) }
        };

        if (rows.Count == 0 || rows.All(item => item.Value <= 0))
        {
            return [];
        }

        var minValue = rows.Min(item => item.Value);
        foreach (var row in rows)
        {
            row.IsBottleneck = Math.Abs(row.Value - minValue) < 0.0005;
        }

        return rows;
    }

    private static string BuildRecommendedInterpretation(
        double emergentRateMax,
        double pipelineCompletionMax,
        double averageTrustMax,
        double effectiveDensityMax,
        double serendipityRateMax,
        double knowledgeReconfigurationMax)
    {
        if (emergentRateMax > 0)
        {
            return "一部条件で創発相が発生しています。最大創発率となったパラメータ値を中心に追加検証してください。";
        }

        if (averageTrustMax < 0.3)
        {
            return "信頼ネットワークが十分に形成されていません。信頼成長率、実効信頼閾値、自然減衰率を確認してください。";
        }

        if (effectiveDensityMax < 0.3)
        {
            return "実効ネットワーク密度が不足しています。信頼閾値または信頼形成条件が厳しすぎる可能性があります。";
        }

        if (serendipityRateMax == 0)
        {
            return "セレンディピティが発生していません。探索傾向、セレンディピティ感受性、閾値を確認してください。";
        }

        if (knowledgeReconfigurationMax < 0.4)
        {
            return "知識再構成が不足しています。異分野接触度、再配線感度、知識再結合率を確認してください。";
        }

        if (pipelineCompletionMax >= 0.7)
        {
            return "創発直前までプロセスは進んでいますが、最終的な相変化には届いていません。ボトルネック段階を調整してください。";
        }

        return "信頼・セレンディピティ・知識再構成の連鎖が弱く、創発プロセスが中途で停滞しています。";
    }

    private static string BuildPrimaryConclusionMessage(
        IReadOnlyList<SensitivityRankingRow> ranking,
        double emergentRateMax,
        double pipelineCompletionMax)
    {
        if (emergentRateMax <= 0 && pipelineCompletionMax >= 0.7)
        {
            return "創発プロセスは進行していますが、最終的な相変化には至っていません。";
        }

        if (emergentRateMax <= 0 && pipelineCompletionMax < 0.7)
        {
            return "創発プロセスの初期段階で停滞しています。";
        }

        var top = ranking.FirstOrDefault();
        if (top is null)
        {
            return "--";
        }

        return top.Metric switch
        {
            "セレンディピティ率" => "このスイープでは、対象パラメータは主にセレンディピティ発生率に影響しています。",
            "知識再構成" => "このスイープでは、対象パラメータは主に知識再構成に影響しています。",
            "平均信頼度" or "実効密度" => "このスイープでは、対象パラメータは主に信頼ネットワーク形成に影響しています。",
            "EmergentRate" => "このスイープでは、対象パラメータが創発相への到達に直接影響しています。",
            _ => "このスイープでは、対象パラメータは複数の中間指標に影響しています。"
        };
    }

    private static string BuildMostCommonPipelineBottleneck(IReadOnlyCollection<string> bottlenecks)
    {
        if (bottlenecks.Count == 0)
        {
            return "--";
        }

        return bottlenecks
            .GroupBy(item => item)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.Key)
            .FirstOrDefault() ?? "--";
    }

    private static string BuildMaxMetricSummary(
        IEnumerable<ParameterSweepAnalysisPoint> points,
        Func<ParameterSweepAnalysisPoint, double> selector,
        string metricLabel)
    {
        var point = points
            .OrderByDescending(selector)
            .ThenBy(item => item.ParameterValue)
            .FirstOrDefault();

        return point is null
            ? "--"
            : $"{point.ParameterValue:0.000}（{metricLabel} {selector(point):0.000}）";
    }

    private static string BuildMaxDeltaSummary(
        IEnumerable<ParameterSweepAnalysisPoint> points,
        Func<ParameterSweepAnalysisPoint, double?> selector)
    {
        var point = points
            .Where(item => selector(item).HasValue)
            .OrderByDescending(item => selector(item)!.Value)
            .ThenBy(item => item.ParameterValue)
            .FirstOrDefault();

        if (point is null || !point.PreviousParameterValue.HasValue || !selector(point).HasValue)
        {
            return "--";
        }

        return $"{point.PreviousParameterValue.Value:0.000} → {point.ParameterValue:0.000}（{FormatSignedDelta(selector(point)!.Value, false)}）";
    }

    private static double RoundAverage(IEnumerable<double> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : Math.Round(list.Average(), 3);
    }

    private static string DetermineEmergentFailureReason(
        ProjectAnalysisResult analysis,
        out bool trustInsufficient,
        out bool effectiveDensityInsufficient,
        out bool strongLinkInsufficient,
        out bool knowledgeDiversityInsufficient,
        out bool knowledgeRecombinationInsufficient,
        out bool knowledgeReconfigurationInsufficient,
        out bool serendipityInsufficient,
        out bool ideaProposalInsufficient,
        out bool shareInfoInsufficient,
        out bool psychologicalSafetyInsufficient,
        out bool constructiveCriticismInsufficient)
    {
        trustInsufficient = false;
        effectiveDensityInsufficient = false;
        strongLinkInsufficient = false;
        knowledgeDiversityInsufficient = false;
        knowledgeRecombinationInsufficient = false;
        knowledgeReconfigurationInsufficient = false;
        serendipityInsufficient = false;
        ideaProposalInsufficient = false;
        shareInfoInsufficient = false;
        psychologicalSafetyInsufficient = false;
        constructiveCriticismInsufficient = false;

        if (analysis.FinalKnowledgePoint is not null
            && string.Equals(analysis.FinalKnowledgePoint.Phase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        var criteria = BuildEmergentCriteria(analysis);
        trustInsufficient = !criteria["AverageTrust"];
        effectiveDensityInsufficient = !criteria["EffectiveDensity"];
        strongLinkInsufficient = !criteria["StrongLinks"];
        knowledgeDiversityInsufficient = !criteria["KnowledgeDiversity"];
        knowledgeRecombinationInsufficient = !criteria["KnowledgeRecombination"];
        knowledgeReconfigurationInsufficient = !criteria["KnowledgeReconfiguration"];
        serendipityInsufficient = !criteria["Serendipity"];
        ideaProposalInsufficient = !criteria["ProposeIdea"];
        shareInfoInsufficient = !criteria["ShareInfo"];
        psychologicalSafetyInsufficient = !criteria["PsychologicalSafety"];
        constructiveCriticismInsufficient = !criteria["ConstructiveCriticism"];

        if (analysis.FinalKnowledgePoint is null)
        {
            return "データ不足";
        }

        if (trustInsufficient)
        {
            return "信頼不足";
        }

        if (effectiveDensityInsufficient)
        {
            return "実効密度不足";
        }

        if (strongLinkInsufficient)
        {
            return "StrongLink不足";
        }

        if (knowledgeDiversityInsufficient)
        {
            return "知識多様性不足";
        }

        if (knowledgeRecombinationInsufficient)
        {
            return "知識再結合不足";
        }

        if (knowledgeReconfigurationInsufficient)
        {
            return "知識再構成不足";
        }

        if (serendipityInsufficient)
        {
            return "セレンディピティ不足";
        }

        if (ideaProposalInsufficient)
        {
            return "アイデア提案不足";
        }

        if (shareInfoInsufficient)
        {
            return "情報共有不足";
        }

        if (psychologicalSafetyInsufficient)
        {
            return "心理的安全性不足";
        }

        if (constructiveCriticismInsufficient)
        {
            return "建設的批判不足";
        }

        if (string.Equals(analysis.FinalKnowledgePoint.Phase, SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase)
            || analysis.FinalKnowledgePoint.StableScore >= analysis.FinalKnowledgePoint.EmergentScore)
        {
            return "Stable優勢";
        }

        if (string.Equals(analysis.FinalKnowledgePoint.Phase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase))
        {
            return "Learning優勢";
        }

        return "未達要因複合";
    }

    private static Dictionary<string, bool> BuildEmergentCriteria(ProjectAnalysisResult analysis)
    {
        var finalPoint = analysis.FinalKnowledgePoint;
        var averageTrust = analysis.Metrics.AverageTrust;
        var effectiveDensity = analysis.Metrics.EffectiveNetworkDensity;
        var strongLinkThreshold = Math.Max(analysis.AgentCount * 2, 1);
        var knowledgeDiversity = finalPoint?.KnowledgeDiversity ?? 0;
        var knowledgeRecombination = finalPoint?.KnowledgeRecombinationScore ?? 0;
        var knowledgeReconfiguration = finalPoint?.KnowledgeReconfigurationScore ?? 0;
        var serendipityScore = finalPoint?.SerendipityScore ?? 0;
        var serendipityOccurred = finalPoint?.SerendipityOccurred ?? false;
        var constructiveCriticismRate = KnowledgeAnalysisService.CalculateConstructiveCriticismRate(
            analysis.ActionDistribution.CriticizeRate,
            analysis.PsychologicalSafetyLevel);

        return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["AverageTrust"] = averageTrust >= 0.30,
            ["EffectiveDensity"] = effectiveDensity >= 0.30,
            ["StrongLinks"] = analysis.Metrics.StrongLinkCount >= strongLinkThreshold,
            ["KnowledgeDiversity"] = knowledgeDiversity >= 0.60,
            ["KnowledgeRecombination"] = knowledgeRecombination >= 0.25,
            ["KnowledgeReconfiguration"] = knowledgeReconfiguration >= 0.25,
            ["Serendipity"] = serendipityOccurred || serendipityScore >= 0.30,
            ["ProposeIdea"] = analysis.ActionDistribution.ProposeIdeaRate >= 0.07,
            ["ShareInfo"] = analysis.ActionDistribution.ShareInfoRate >= 0.30,
            ["PsychologicalSafety"] = analysis.PsychologicalSafetyLevel >= 0.60,
            ["ConstructiveCriticism"] = constructiveCriticismRate >= 0.08 && analysis.PsychologicalSafetyLevel >= 0.60
        };
    }

    private sealed class ProjectAnalysisResult
    {
        public NetworkMetricsResult Metrics { get; init; } = new();
        public double AverageSerendipityScore { get; init; }
        public double AverageKnowledgeDiversity { get; init; }
        public double AverageKnowledgeRecombinationScore { get; init; }
        public double AverageKnowledgeReconfigurationScore { get; init; }
        public double AverageLearningScore { get; init; }
        public double AverageAdaptationScore { get; init; }
        public double AverageEmergentScore { get; init; }
        public double AveragePipelineCompletionScore { get; init; }
        public string MostCommonPipelineBottleneck { get; init; } = "--";
        public bool SerendipityOccurred { get; init; }
        public bool SerendipityToEmergenceLink { get; init; }
        public KnowledgeTimelinePoint? FinalKnowledgePoint { get; init; }
        public ActionDistributionSummary ActionDistribution { get; init; } = new();
        public int AgentCount { get; init; }
        public double PsychologicalSafetyLevel { get; init; }
    }

    private static Dictionary<string, int> DeserializePhaseSummary(string json)
    {
        var defaults = new[]
        {
            SimulationPhase.Forming,
            SimulationPhase.Learning,
            SimulationPhase.Stable,
            SimulationPhase.Emergent,
            SimulationPhase.Silo,
            SimulationPhase.Chaos,
            SimulationPhase.Collapse
        }.ToDictionary(item => item, _ => 0);

        if (string.IsNullOrWhiteSpace(json))
        {
            return defaults;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
            foreach (var key in defaults.Keys.ToList())
            {
                defaults[key] = values.GetValueOrDefault(key, 0);
            }
        }
        catch
        {
            return defaults;
        }

        return defaults;
    }
}

public sealed record class ImpactPathNode
{
    public string Title { get; init; } = "";
    public string ValueLine { get; init; } = "";
    public string NoteLine { get; init; } = "";
    public string CssClass { get; init; } = "";
}

public sealed record class SensitivityRankingRow
{
    public int Rank { get; init; }
    public string Metric { get; init; } = "";
    public double MinValue { get; init; }
    public double MaxValue { get; init; }
    public double Delta { get; init; }
    public double AbsoluteDelta { get; init; }
    public string ImpactLabel { get; init; } = "";
    public string Interpretation { get; init; } = "";
    public string CssClass { get; init; } = "";
}

public sealed class PipelineStageSummaryRow
{
    public string Label { get; set; } = "";
    public double Value { get; set; }
    public bool IsBottleneck { get; set; }
}
