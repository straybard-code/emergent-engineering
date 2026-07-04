using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class DetailsModel(AppDbContext db) : PageModel
{
    [TempData]
    public string? SweepMessage { get; set; }

    public ParameterSweep? Sweep { get; private set; }
    public string ScenarioName { get; private set; } = "-";
    public List<ParameterSweepRun> Runs { get; private set; } = [];
    public List<ParameterSweepPhaseRow> PhaseComparison { get; private set; } = [];
    public List<ParameterSweepAnalysisPoint> AnalysisPoints { get; private set; } = [];
    public Dictionary<int, string> ExperimentNames { get; private set; } = [];
    public string MaxAverageTrustSummary { get; private set; } = "--";
    public string MaxEffectiveDensitySummary { get; private set; } = "--";
    public string MaxEmergentRateSummary { get; private set; } = "--";
    public string MaxSerendipityRateSummary { get; private set; } = "--";
    public string MaxTrustJumpSummary { get; private set; } = "--";
    public string MaxEmergenceJumpSummary { get; private set; } = "--";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
        if (Sweep is null)
        {
            return RedirectToPage("/ParameterSweeps/Index");
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
        var stepsByProjectId = steps
            .GroupBy(item => item.SimulationProjectId)
            .ToDictionary(group => group.Key, group => group.ToList());

        List<ParameterSweepAnalysisPoint> points = [];
        foreach (var sweepRun in Runs)
        {
            var experimentProjects = projectsByExperimentId.GetValueOrDefault(sweepRun.ExperimentId, []);
            var projectRuns = runsByExperimentId.GetValueOrDefault(sweepRun.ExperimentId, []);
            var analyses = experimentProjects
                .Select(project => AnalyzeProject(project, stepsByProjectId.GetValueOrDefault(project.Id, [])))
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
            SerendipityToEmergenceRate = totalRunCount == 0 ? 0 : Math.Round(analyses.Count(item => item.SerendipityToEmergenceLink) / (double)totalRunCount, 3)
        };
    }

    private static ProjectAnalysisResult AnalyzeProject(SimulationProject project, IReadOnlyCollection<SimulationStep> steps)
    {
        var metrics = NetworkMetricsCalculator.CalculateFromAgents(project.Agents, project.EffectiveTrustThreshold);
        var knowledgeTimeline = KnowledgeAnalysisService.BuildTimeline(steps);
        var averageSerendipityScore = knowledgeTimeline.Count == 0
            ? 0
            : Math.Round(knowledgeTimeline.Average(item => item.SerendipityScore), 3);

        return new ProjectAnalysisResult
        {
            Metrics = metrics,
            AverageSerendipityScore = averageSerendipityScore,
            SerendipityOccurred = knowledgeTimeline.Any(item => item.SerendipityOccurred),
            SerendipityToEmergenceLink = knowledgeTimeline.Any(item => item.SerendipityToEmergenceLink)
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

        return $"{point.PreviousParameterValue.Value:0.000} → {point.ParameterValue:0.000}（{FormatSignedDelta(selector(point)!.Value)}）";
    }

    private static string FormatSignedDelta(double value)
    {
        return value >= 0 ? $"+{value:0.000}" : value.ToString("0.000");
    }

    private static double RoundAverage(IEnumerable<double> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : Math.Round(list.Average(), 3);
    }

    private sealed class ProjectAnalysisResult
    {
        public NetworkMetricsResult Metrics { get; init; } = new();
        public double AverageSerendipityScore { get; init; }
        public bool SerendipityOccurred { get; init; }
        public bool SerendipityToEmergenceLink { get; init; }
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
