using System.Globalization;
using System.Net;
using System.Text;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Services;

public sealed class ExperimentReportService(
    AppDbContext db,
    IWebHostEnvironment webHostEnvironment)
{
    public async Task<ExperimentReportResult> CreateReportAsync(int experimentId, CancellationToken cancellationToken = default)
    {
        var experiment = await db.Experiments
            .AsNoTracking()
            .Include(item => item.Scenario)
            .FirstOrDefaultAsync(item => item.Id == experimentId, cancellationToken);

        if (experiment is null)
        {
            throw new InvalidOperationException("Experimentが見つかりませんでした。");
        }

        var runs = await db.ExperimentRuns
            .AsNoTracking()
            .Where(item => item.ExperimentId == experimentId)
            .OrderBy(item => item.RunNo)
            .ToListAsync(cancellationToken);

        var simulationProjects = await db.SimulationProjects
            .AsNoTracking()
            .Where(item => item.ExperimentId == experimentId)
            .Include(item => item.Steps)
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

        var projectSummaries = BuildProjectSummaries(simulationProjects, runs);
        var metrics = BuildMetrics(runs, projectSummaries);
        var interpretation = BuildInterpretation(metrics);
        var generatedAt = DateTime.UtcNow;
        var html = BuildHtml(experiment, runs, projectSummaries, metrics, interpretation, generatedAt);

        var reportDirectory = Path.Combine(GetWebRootPath(), "reports", "experiments");
        Directory.CreateDirectory(reportDirectory);

        var fileName = $"experiment-{experimentId}-{generatedAt:yyyyMMddHHmmss}.html";
        var reportPath = Path.Combine(reportDirectory, fileName);
        await File.WriteAllTextAsync(reportPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);

        return new ExperimentReportResult(
            FileName: fileName,
            RelativeUrl: $"/reports/experiments/{fileName}",
            AbsolutePath: reportPath,
            CreatedAt: generatedAt);
    }

    private static IReadOnlyList<ProjectReportSummary> BuildProjectSummaries(
        IReadOnlyList<SimulationProject> projects,
        IReadOnlyList<ExperimentRun> runs)
    {
        var runByProjectId = runs
            .GroupBy(item => item.SimulationProjectId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.RunNo).First());

        return projects
            .Select(project =>
            {
                var timeline = KnowledgeAnalysisService.BuildTimeline(
                    project.Steps
                        .OrderBy(step => step.StepNo)
                        .ToList());

                runByProjectId.TryGetValue(project.Id, out var run);
                var timelineCount = timeline.Count;
                var totalThanksCount = timeline.Sum(item => item.ThanksCoinCount);

                return new ProjectReportSummary
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    RunNo = run?.RunNo,
                    FinalPhase = run?.FinalPhase ?? project.Metrics?.FinalPhase ?? project.Phase,
                    AverageTrust = run?.AverageTrust ?? project.Metrics?.AverageTrust ?? 0,
                    EffectiveDensity = run?.EffectiveNetworkDensity ?? project.Metrics?.EffectiveNetworkDensity ?? 0,
                    ComponentCount = run?.ComponentCount ?? project.Metrics?.ComponentCount ?? 0,
                    PhaseStability = run?.PhaseStability ?? project.Metrics?.PhaseStability ?? 0,
                    CompletedSteps = run?.CompletedSteps ?? project.CurrentStep,
                    KnowledgeReconfigurationScore = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.KnowledgeReconfigurationScore), 3),
                    SerendipityRate = timelineCount == 0 ? 0 : Math.Round(timeline.Count(item => item.SerendipityOccurred) / (double)timelineCount, 3),
                    AverageRespect = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.AverageRespect), 3),
                    RespectDensity = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.RespectDensity), 3),
                    RespectConcentration = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.RespectConcentration), 3),
                    AverageIntellectualRespect = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.AverageIntellectualRespect), 3),
                    IntellectualRespectDensity = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.IntellectualRespectDensity), 3),
                    IntellectualRespectConcentration = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.IntellectualRespectConcentration), 3),
                    ChallengeAcceptanceScore = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.ChallengeAcceptanceScore), 3),
                    MutualMentorshipScore = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.MutualMentorshipScore), 3),
                    ThanksOccurrenceRate = timelineCount == 0 ? 0 : Math.Round(timeline.Count(item => item.ThanksCoinOccurred) / (double)timelineCount, 3),
                    AverageThanksCoinCount = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.ThanksCoinCount), 3),
                    ThanksChallengeRate = totalThanksCount == 0 ? 0 : Math.Round(timeline.Sum(item => item.ThanksCoinChallengeCount) / (double)totalThanksCount, 3),
                    ThanksBridgeRate = totalThanksCount == 0 ? 0 : Math.Round(timeline.Sum(item => item.ThanksCoinBridgeCount) / (double)totalThanksCount, 3),
                    ThanksConcentration = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.ThanksConcentration), 3),
                    ThanksToEmergenceContribution = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.ThanksCoinToEmergenceContribution), 3),
                    AverageEgoPenaltyApplied = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.EgoPenaltyApplied), 3),
                    AverageHierarchyPenaltyApplied = timelineCount == 0 ? 0 : Math.Round(timeline.Average(item => item.HierarchyPenaltyApplied), 3),
                    LearnedFromUnexpectedAgentCount = timelineCount == 0 ? 0 : (int)Math.Round(timeline.Average(item => item.LearnedFromUnexpectedAgentCount), 0)
                };
            })
            .ToList();
    }

    private static ExperimentReportMetrics BuildMetrics(
        IReadOnlyList<ExperimentRun> runs,
        IReadOnlyList<ProjectReportSummary> projectSummaries)
    {
        var totalRuns = runs.Count;
        var emergentRuns = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase));
        var stableRuns = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Stable, StringComparison.OrdinalIgnoreCase));
        var learningRuns = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase));
        var adaptationRuns = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Adaptation, StringComparison.OrdinalIgnoreCase));
        var formingRuns = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Forming, StringComparison.OrdinalIgnoreCase));
        var siloRuns = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Silo, StringComparison.OrdinalIgnoreCase));
        var chaosRuns = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Chaos, StringComparison.OrdinalIgnoreCase));
        var collapseRuns = runs.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Collapse, StringComparison.OrdinalIgnoreCase));

        return new ExperimentReportMetrics
        {
            TotalRuns = totalRuns,
            EmergentRuns = emergentRuns,
            StableRuns = stableRuns,
            LearningRuns = learningRuns,
            AdaptationRuns = adaptationRuns,
            FormingRuns = formingRuns,
            SiloRuns = siloRuns,
            ChaosRuns = chaosRuns,
            CollapseRuns = collapseRuns,
            EmergentRate = totalRuns == 0 ? 0 : Math.Round(emergentRuns / (double)totalRuns, 3),
            StableRate = totalRuns == 0 ? 0 : Math.Round(stableRuns / (double)totalRuns, 3),
            LearningRate = totalRuns == 0 ? 0 : Math.Round(learningRuns / (double)totalRuns, 3),
            AverageTrust = totalRuns == 0 ? 0 : Math.Round(runs.Average(item => item.AverageTrust), 3),
            AverageEffectiveDensity = totalRuns == 0 ? 0 : Math.Round(runs.Average(item => item.EffectiveNetworkDensity), 3),
            AverageComponentCount = totalRuns == 0 ? 0 : Math.Round(runs.Average(item => item.ComponentCount), 3),
            AveragePhaseStability = totalRuns == 0 ? 0 : Math.Round(runs.Average(item => item.PhaseStability), 3),
            AverageKnowledgeReconfiguration = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.KnowledgeReconfigurationScore), 3),
            AverageSerendipityRate = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.SerendipityRate), 3),
            AverageRespect = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.AverageRespect), 3),
            AverageRespectDensity = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.RespectDensity), 3),
            AverageRespectConcentration = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.RespectConcentration), 3),
            AverageIntellectualRespect = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.AverageIntellectualRespect), 3),
            AverageIntellectualRespectDensity = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.IntellectualRespectDensity), 3),
            AverageIntellectualRespectConcentration = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.IntellectualRespectConcentration), 3),
            AverageChallengeAcceptanceScore = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.ChallengeAcceptanceScore), 3),
            AverageMutualMentorshipScore = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.MutualMentorshipScore), 3),
            AverageThanksOccurrenceRate = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.ThanksOccurrenceRate), 3),
            AverageThanksCoinCount = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.AverageThanksCoinCount), 3),
            AverageThanksChallengeRate = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.ThanksChallengeRate), 3),
            AverageThanksBridgeRate = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.ThanksBridgeRate), 3),
            AverageThanksConcentration = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.ThanksConcentration), 3),
            AverageThanksToEmergenceContribution = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.ThanksToEmergenceContribution), 3),
            AverageEgoPenaltyApplied = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.AverageEgoPenaltyApplied), 3),
            AverageHierarchyPenaltyApplied = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.AverageHierarchyPenaltyApplied), 3),
            AverageLearnedFromUnexpectedAgentCount = projectSummaries.Count == 0 ? 0 : Math.Round(projectSummaries.Average(item => item.LearnedFromUnexpectedAgentCount), 3)
        };
    }

    private static string BuildInterpretation(ExperimentReportMetrics metrics)
    {
        List<string> comments = [];

        if (metrics.EmergentRate >= 0.25)
        {
            comments.Add("創発相が十分に観測されており、相互敬意と知的敬意が結果に結びついている可能性があります。");
        }

        if (metrics.AverageIntellectualRespect >= 0.40 && metrics.AverageChallengeAcceptanceScore >= 0.55)
        {
            comments.Add("知的敬意と異論受容が高く、知識再構成を支える土台が見えています。");
        }

        if (metrics.AverageRespect >= 0.50 && metrics.AverageIntellectualRespect < 0.35)
        {
            comments.Add("人格的な相互敬意は強い一方で、知的敬意の伸びは控えめです。");
        }

        if (metrics.AverageThanksConcentration >= 0.60)
        {
            comments.Add("Thanks の集中が高く、人気投票化の兆候に注意が必要です。");
        }

        if (metrics.AverageSerendipityRate >= 0.20)
        {
            comments.Add("セレンディピティの発生が一定以上あり、予期しない組み替えが起きています。");
        }

        if (comments.Count == 0)
        {
            comments.Add("この実験は、学習・安定・創発の境界を確認する基準ケースとして参照できます。");
        }

        return string.Join(" ", comments);
    }

    private string BuildHtml(
        Experiment experiment,
        IReadOnlyList<ExperimentRun> runs,
        IReadOnlyList<ProjectReportSummary> projectSummaries,
        ExperimentReportMetrics metrics,
        string interpretation,
        DateTime generatedAt)
    {
        var phaseDistribution = BuildPhaseDistribution(runs);
        var representativeRuns = SelectRepresentativeRuns(projectSummaries);

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"ja\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine($"<title>{Encode(experiment.Name)} - Experiment Report</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("""
            :root {
                color-scheme: light;
                --border: #d1d5db;
                --muted: #6b7280;
                --bg: #f8fafc;
                --card: #ffffff;
                --title: #0f172a;
                --accent: #2563eb;
                --accent-soft: #dbeafe;
            }
            * { box-sizing: border-box; }
            body {
                margin: 0;
                padding: 32px;
                background: var(--bg);
                color: #111827;
                font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Hiragino Kaku Gothic ProN", "Yu Gothic", sans-serif;
                line-height: 1.6;
            }
            .container { max-width: 1280px; margin: 0 auto; }
            .header {
                background: linear-gradient(135deg, #eff6ff, #ffffff);
                border: 1px solid var(--border);
                border-radius: 14px;
                padding: 28px;
                margin-bottom: 24px;
            }
            h1, h2, h3 { color: var(--title); margin-top: 0; }
            h1 { margin-bottom: 8px; font-size: 2rem; }
            h2 { margin-bottom: 16px; font-size: 1.35rem; }
            h3 { margin-bottom: 12px; font-size: 1.1rem; }
            .muted { color: var(--muted); }
            .section {
                background: var(--card);
                border: 1px solid var(--border);
                border-radius: 14px;
                padding: 24px;
                margin-bottom: 24px;
                box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);
            }
            .cards {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
                gap: 12px;
            }
            .card {
                border: 1px solid var(--border);
                border-radius: 12px;
                padding: 14px 16px;
                background: #fff;
            }
            .card .label {
                display: block;
                color: var(--muted);
                font-size: 0.85rem;
                margin-bottom: 6px;
            }
            .card .value {
                display: block;
                font-size: 1.1rem;
                font-weight: 700;
                color: var(--title);
            }
            table {
                width: 100%;
                border-collapse: collapse;
                border-spacing: 0;
            }
            th, td {
                border: 1px solid var(--border);
                padding: 10px 12px;
                text-align: left;
                vertical-align: top;
            }
            th {
                background: #eff6ff;
                color: #1e3a8a;
                font-weight: 700;
            }
            tbody tr:nth-child(even) td { background: #fafafa; }
            .table-wrap { overflow-x: auto; }
            .pill {
                display: inline-block;
                padding: 4px 10px;
                border-radius: 999px;
                background: var(--accent-soft);
                color: #1d4ed8;
                font-size: 0.85rem;
                font-weight: 700;
            }
            .section-note {
                margin: 0 0 16px;
                color: var(--muted);
            }
            .two-column {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
                gap: 20px;
            }
            .summary-block {
                display: grid;
                gap: 8px;
            }
            .report-footer {
                color: var(--muted);
                font-size: 0.9rem;
                margin-top: 24px;
            }
            """);
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<div class=\"container\">");
        builder.AppendLine("<div class=\"header\">");
        builder.AppendLine($"<div class=\"pill\">Experiment Report</div>");
        builder.AppendLine($"<h1>{Encode(experiment.Name)}</h1>");
        builder.AppendLine($"<p class=\"muted\">Experiment ID: {experiment.Id} / 生成日時: {generatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");
        builder.AppendLine($"<p>{Encode(experiment.Description)}</p>");
        builder.AppendLine("</div>");

        builder.AppendLine(BuildKeyValueSection(
            "基本情報",
            [
                ("Experiment名", experiment.Name),
                ("Scenario名", experiment.Scenario?.Name ?? "-"),
                ("Status", FormatStatus(experiment.Status)),
                ("Purpose", experiment.Purpose),
                ("Boundary Conditions", experiment.BoundaryConditions),
                ("KPI", experiment.KpiDefinition),
                ("Run数", experiment.RunCount.ToString(CultureInfo.InvariantCulture)),
                ("Agent数", experiment.AgentCount.ToString(CultureInfo.InvariantCulture)),
                ("Step数", experiment.TotalSteps.ToString(CultureInfo.InvariantCulture)),
                ("LLM Provider", experiment.LlmProvider),
                ("LLM Model", experiment.LlmModel)
            ]));

        builder.AppendLine(BuildKeyValueSection(
            "実行条件",
            [
                ("情報共有", FormatDouble(experiment.InformationSharingLevel)),
                ("協調性", FormatDouble(experiment.CooperationLevel)),
                ("競争性", FormatDouble(experiment.CompetitionLevel)),
                ("心理的安全性", FormatDouble(experiment.PsychologicalSafetyLevel)),
                ("学習志向", FormatDouble(experiment.LearningOrientationLevel)),
                ("顧客志向", FormatDouble(experiment.CustomerOrientationLevel)),
                ("短期成果圧力", FormatDouble(experiment.ShortTermResultPressureLevel)),
                ("Effective Trust Threshold", FormatDouble(experiment.EffectiveTrustThreshold, 3)),
                ("Knowledge Stock", FormatDouble(experiment.KnowledgeStock, 3)),
                ("Knowledge Diversity", FormatDouble(experiment.KnowledgeDiversity, 3)),
                ("External Shock", FormatDouble(experiment.ExternalShockLevel, 3)),
                ("Cross Domain Exposure", FormatDouble(experiment.CrossDomainExposure, 3)),
                ("Rewiring Sensitivity", FormatDouble(experiment.RewiringSensitivity, 3)),
                ("Enable External Shock", FormatBool(experiment.EnableExternalShock)),
                ("Shock Step", experiment.ShockStep.ToString(CultureInfo.InvariantCulture)),
                ("Shock Type", experiment.ShockType),
                ("Shock Description", experiment.ShockDescription),
                ("Enable Challenge Event", FormatBool(experiment.EnableChallengeEvent)),
                ("Challenge Type", experiment.ChallengeType),
                ("Challenge Step", experiment.ChallengeStep.ToString(CultureInfo.InvariantCulture)),
                ("Challenge Level", FormatDouble(experiment.ChallengeLevel, 3)),
                ("Challenge Description", experiment.ChallengeDescription),
                ("Enable Serendipity", FormatBool(experiment.EnableSerendipity)),
                ("Exploration Tendency", FormatDouble(experiment.ExplorationTendency, 3)),
                ("Serendipity Sensitivity", FormatDouble(experiment.SerendipitySensitivity, 3)),
                ("Knowledge Recombination Rate", FormatDouble(experiment.KnowledgeRecombinationRate, 3)),
                ("Serendipity Threshold", FormatDouble(experiment.SerendipityThreshold, 3)),
                ("Enable Trust Dynamics", FormatBool(experiment.EnableTrustDynamics)),
                ("Trust Growth Rate", FormatDouble(experiment.TrustGrowthRate, 3)),
                ("Trust Decay Rate", FormatDouble(experiment.TrustDecayRate, 3)),
                ("Trust Saturation Strength", FormatDouble(experiment.TrustSaturationStrength, 3)),
                ("Trust Capacity", experiment.TrustCapacity.ToString(CultureInfo.InvariantCulture)),
                ("Trust Capacity Penalty", FormatDouble(experiment.TrustCapacityPenalty, 3)),
                ("Distrust Penalty", FormatDouble(experiment.DistrustPenalty, 3)),
                ("Constructive Criticism Bonus", FormatDouble(experiment.ConstructiveCriticismBonus, 3))
            ]));

        builder.AppendLine(BuildKeyValueSection(
            "Thanks Coin / 相互敬意 / 知的敬意",
            [
                ("Thanks Coin 有効", FormatBool(experiment.EnableThanksCoin)),
                ("Thanks Coin 発生率", FormatDouble(experiment.ThanksCoinRate, 3)),
                ("敬意増加量", FormatDouble(experiment.ThanksCoinRespectGain, 3)),
                ("信頼増加量", FormatDouble(experiment.ThanksCoinTrustGain, 3)),
                ("知識再構成増加量", FormatDouble(experiment.ThanksCoinReconfigurationGain, 3)),
                ("心理的安全性増加量", FormatDouble(experiment.ThanksCoinPsychologicalSafetyGain, 3)),
                ("橋渡し増加量", FormatDouble(experiment.ThanksCoinBridgeGain, 3)),
                ("人気集中バイアス", FormatDouble(experiment.ThanksCoinPopularityBias, 3)),
                ("多様性ボーナス", FormatDouble(experiment.ThanksCoinDiversityBonus, 3)),
                ("建設的異論ボーナス", FormatDouble(experiment.ThanksCoinChallengeBonus, 3)),
                ("相互敬意ベース", FormatDouble(experiment.MutualRespectBase, 3)),
                ("相互敬意成長率", FormatDouble(experiment.MutualRespectGrowthRate, 3)),
                ("相互敬意減衰率", FormatDouble(experiment.MutualRespectDecayRate, 3)),
                ("異質性尊重感度", FormatDouble(experiment.MutualRespectDiversitySensitivity, 3)),
                ("異論尊重感度", FormatDouble(experiment.MutualRespectChallengeSensitivity, 3)),
                ("橋渡し尊重感度", FormatDouble(experiment.MutualRespectBridgeSensitivity, 3)),
                ("人気集中ペナルティ", FormatDouble(experiment.MutualRespectPopularityPenalty, 3)),
                ("知的敬意 有効", FormatBool(experiment.EnableIntellectualRespect)),
                ("知的敬意ベース", FormatDouble(experiment.IntellectualRespectBase, 3)),
                ("知的敬意成長率", FormatDouble(experiment.IntellectualRespectGrowthRate, 3)),
                ("知的敬意減衰率", FormatDouble(experiment.IntellectualRespectDecayRate, 3)),
                ("異質知識尊重感度", FormatDouble(experiment.IntellectualRespectDiversitySensitivity, 3)),
                ("異論知的敬意感度", FormatDouble(experiment.IntellectualRespectChallengeSensitivity, 3)),
                ("師匠化感度", FormatDouble(experiment.IntellectualRespectMentorshipSensitivity, 3)),
                ("自尊心ペナルティ", FormatDouble(experiment.IntellectualRespectEgoPenalty, 3)),
                ("序列ペナルティ", FormatDouble(experiment.IntellectualRespectHierarchyPenalty, 3))
            ]));

        builder.AppendLine(BuildSummarySection(metrics));
        builder.AppendLine(BuildPhaseDistributionSection(phaseDistribution, metrics.TotalRuns));
        builder.AppendLine(BuildRepresentativeRunsSection(representativeRuns));
        builder.AppendLine(BuildInterpretationSection(interpretation));

        builder.AppendLine("<div class=\"report-footer\">");
        builder.AppendLine("このHTMLレポートは削除されるExperimentとは独立して保存されます。");
        builder.AppendLine("</div>");

        builder.AppendLine("</div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string BuildKeyValueSection(string title, IReadOnlyCollection<(string Label, string Value)> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine($"<h2>{Encode(title)}</h2>");
        builder.AppendLine("<div class=\"table-wrap\">");
        builder.AppendLine("<table>");
        builder.AppendLine("<tbody>");
        foreach (var (label, value) in rows)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<th style=\"width: 280px;\">{Encode(label)}</th>");
            builder.AppendLine($"<td>{Encode(value)}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildSummarySection(ExperimentReportMetrics metrics)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>サマリー</h2>");
        builder.AppendLine("<div class=\"cards\">");

        AppendCard(builder, "Run数", $"{metrics.TotalRuns}");
        AppendCard(builder, "創発率", FormatDouble(metrics.EmergentRate));
        AppendCard(builder, "安定率", FormatDouble(metrics.StableRate));
        AppendCard(builder, "学習率", FormatDouble(metrics.LearningRate));
        AppendCard(builder, "平均信頼度", FormatDouble(metrics.AverageTrust));
        AppendCard(builder, "平均Effective Density", FormatDouble(metrics.AverageEffectiveDensity));
        AppendCard(builder, "平均Knowledge Reconfiguration", FormatDouble(metrics.AverageKnowledgeReconfiguration));
        AppendCard(builder, "平均Serendipity Rate", FormatDouble(metrics.AverageSerendipityRate));
        AppendCard(builder, "平均相互敬意", FormatDouble(metrics.AverageRespect));
        AppendCard(builder, "平均知的敬意", FormatDouble(metrics.AverageIntellectualRespect));
        AppendCard(builder, "平均Challenge Acceptance", FormatDouble(metrics.AverageChallengeAcceptanceScore));
        AppendCard(builder, "平均Mutual Mentorship", FormatDouble(metrics.AverageMutualMentorshipScore));
        AppendCard(builder, "平均Thanks集中度", FormatDouble(metrics.AverageThanksConcentration));
        AppendCard(builder, "平均Thanks→創発寄与", FormatDouble(metrics.AverageThanksToEmergenceContribution));

        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildPhaseDistributionSection(IReadOnlyCollection<PhaseCountRow> rows, int totalRuns)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>Phase分布</h2>");
        builder.AppendLine("<div class=\"table-wrap\">");
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>Phase</th><th>件数</th><th>比率</th></tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var row in rows)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{Encode(row.Label)}</td>");
            builder.AppendLine($"<td>{row.Count}</td>");
            builder.AppendLine($"<td>{(totalRuns == 0 ? 0 : row.Count / (double)totalRuns):0.000}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildRepresentativeRunsSection(IReadOnlyList<ProjectReportSummary> runs)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>代表的なRun一覧</h2>");
        builder.AppendLine("<p class=\"section-note\">Final Phase と信頼・敬意系指標から代表Runを抽出しています。</p>");

        if (runs.Count == 0)
        {
            builder.AppendLine("<p class=\"muted\">Runがありません。</p>");
            builder.AppendLine("</section>");
            return builder.ToString();
        }

        var displayedRuns = runs
            .OrderByDescending(item => string.Equals(item.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
            .ThenBy(item => GetPhaseOrder(item.FinalPhase))
            .ThenByDescending(item => item.AverageTrust)
            .ThenBy(item => item.RunNo ?? int.MaxValue)
            .Take(8)
            .ToList();

        builder.AppendLine("<div class=\"table-wrap\">");
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>Run</th><th>Simulation</th><th>Final Phase</th><th>平均信頼度</th><th>Effective Density</th><th>Component</th><th>Phase Stability</th><th>相互敬意</th><th>知的敬意</th><th>Mutual Mentorship</th><th>Step</th></tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var item in displayedRuns)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{item.RunNo?.ToString(CultureInfo.InvariantCulture) ?? "-"}</td>");
            builder.AppendLine($"<td>{Encode(item.ProjectName)} (#{item.ProjectId})</td>");
            builder.AppendLine($"<td>{Encode(FormatPhase(item.FinalPhase))}</td>");
            builder.AppendLine($"<td>{FormatDouble(item.AverageTrust)}</td>");
            builder.AppendLine($"<td>{FormatDouble(item.EffectiveDensity)}</td>");
            builder.AppendLine($"<td>{item.ComponentCount}</td>");
            builder.AppendLine($"<td>{FormatDouble(item.PhaseStability)}</td>");
            builder.AppendLine($"<td>{FormatDouble(item.AverageRespect)}</td>");
            builder.AppendLine($"<td>{FormatDouble(item.AverageIntellectualRespect)}</td>");
            builder.AppendLine($"<td>{FormatDouble(item.MutualMentorshipScore)}</td>");
            builder.AppendLine($"<td>{item.CompletedSteps}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");

        if (runs.Count > displayedRuns.Count)
        {
            builder.AppendLine($"<p class=\"section-note\">ほか {runs.Count - displayedRuns.Count} 件のRunがあります。</p>");
        }

        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildInterpretationSection(string interpretation)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>解釈コメント</h2>");
        builder.AppendLine($"<p>{Encode(interpretation)}</p>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static void AppendCard(StringBuilder builder, string label, string value)
    {
        builder.AppendLine("<div class=\"card\">");
        builder.AppendLine($"<span class=\"label\">{Encode(label)}</span>");
        builder.AppendLine($"<span class=\"value\">{Encode(value)}</span>");
        builder.AppendLine("</div>");
    }

    private static List<PhaseCountRow> BuildPhaseDistribution(IReadOnlyList<ExperimentRun> runs)
    {
        var orderedPhases = new[]
        {
            SimulationPhase.Emergent,
            SimulationPhase.Stable,
            SimulationPhase.Learning,
            SimulationPhase.Adaptation,
            SimulationPhase.Forming,
            SimulationPhase.Silo,
            SimulationPhase.Chaos,
            SimulationPhase.Collapse
        };

        return orderedPhases
            .Select(phase => new PhaseCountRow(
                phase,
                FormatPhase(phase),
                runs.Count(item => string.Equals(item.FinalPhase, phase, StringComparison.OrdinalIgnoreCase))))
            .ToList();
    }

    private static IReadOnlyList<ProjectReportSummary> SelectRepresentativeRuns(IReadOnlyList<ProjectReportSummary> runs)
    {
        return runs
            .OrderByDescending(item => string.Equals(item.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
            .ThenBy(item => GetPhaseOrder(item.FinalPhase))
            .ThenByDescending(item => item.AverageTrust)
            .ThenBy(item => item.RunNo ?? int.MaxValue)
            .Take(8)
            .ToList();
    }

    private static string FormatPhase(string? phase) => phase switch
    {
        SimulationPhase.Forming => "形成期（Forming）",
        SimulationPhase.Learning => "学習期（Learning）",
        SimulationPhase.Adaptation => "適応期（Adaptation）",
        SimulationPhase.Stable => "安定期（Stable）",
        SimulationPhase.Emergent => "創発期（Emergent）",
        SimulationPhase.Silo => "サイロ期（Silo）",
        SimulationPhase.Chaos => "混沌期（Chaos）",
        SimulationPhase.Collapse => "崩壊期（Collapse）",
        _ => phase ?? "-"
    };

    private static string FormatStatus(string? status) => status switch
    {
        ExperimentStatus.Created => "作成済み",
        ExperimentStatus.Running => "実行中",
        ExperimentStatus.StopRequested => "停止要求中",
        ExperimentStatus.Stopped => "停止済み",
        ExperimentStatus.Completed => "完了",
        ExperimentStatus.Failed => "失敗",
        _ => status ?? "-"
    };

    private static int GetPhaseOrder(string? phase) => phase switch
    {
        SimulationPhase.Emergent => 0,
        SimulationPhase.Stable => 1,
        SimulationPhase.Learning => 2,
        SimulationPhase.Adaptation => 3,
        SimulationPhase.Forming => 4,
        SimulationPhase.Silo => 5,
        SimulationPhase.Chaos => 6,
        SimulationPhase.Collapse => 7,
        _ => 99
    };

    private static string FormatBool(bool value) => value ? "有効" : "無効";

    private static string FormatDouble(double value, int digits = 3) => value.ToString($"0.{new string('0', digits)}", CultureInfo.InvariantCulture);

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? "");

    private string GetWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(webHostEnvironment.WebRootPath))
        {
            return webHostEnvironment.WebRootPath;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }
}

public sealed record ExperimentReportResult(
    string FileName,
    string RelativeUrl,
    string AbsolutePath,
    DateTime CreatedAt);

internal sealed class ExperimentReportMetrics
{
    public int TotalRuns { get; init; }
    public int EmergentRuns { get; init; }
    public int StableRuns { get; init; }
    public int LearningRuns { get; init; }
    public int AdaptationRuns { get; init; }
    public int FormingRuns { get; init; }
    public int SiloRuns { get; init; }
    public int ChaosRuns { get; init; }
    public int CollapseRuns { get; init; }
    public double EmergentRate { get; init; }
    public double StableRate { get; init; }
    public double LearningRate { get; init; }
    public double AverageTrust { get; init; }
    public double AverageEffectiveDensity { get; init; }
    public double AverageComponentCount { get; init; }
    public double AveragePhaseStability { get; init; }
    public double AverageKnowledgeReconfiguration { get; init; }
    public double AverageSerendipityRate { get; init; }
    public double AverageRespect { get; init; }
    public double AverageRespectDensity { get; init; }
    public double AverageRespectConcentration { get; init; }
    public double AverageIntellectualRespect { get; init; }
    public double AverageIntellectualRespectDensity { get; init; }
    public double AverageIntellectualRespectConcentration { get; init; }
    public double AverageChallengeAcceptanceScore { get; init; }
    public double AverageMutualMentorshipScore { get; init; }
    public double AverageThanksOccurrenceRate { get; init; }
    public double AverageThanksCoinCount { get; init; }
    public double AverageThanksChallengeRate { get; init; }
    public double AverageThanksBridgeRate { get; init; }
    public double AverageThanksConcentration { get; init; }
    public double AverageThanksToEmergenceContribution { get; init; }
    public double AverageEgoPenaltyApplied { get; init; }
    public double AverageHierarchyPenaltyApplied { get; init; }
    public double AverageLearnedFromUnexpectedAgentCount { get; init; }
}

internal sealed class ProjectReportSummary
{
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = "-";
    public int? RunNo { get; init; }
    public string FinalPhase { get; init; } = SimulationPhase.Forming;
    public double AverageTrust { get; init; }
    public double EffectiveDensity { get; init; }
    public int ComponentCount { get; init; }
    public double PhaseStability { get; init; }
    public int CompletedSteps { get; init; }
    public double KnowledgeReconfigurationScore { get; init; }
    public double SerendipityRate { get; init; }
    public double AverageRespect { get; init; }
    public double RespectDensity { get; init; }
    public double RespectConcentration { get; init; }
    public double AverageIntellectualRespect { get; init; }
    public double IntellectualRespectDensity { get; init; }
    public double IntellectualRespectConcentration { get; init; }
    public double ChallengeAcceptanceScore { get; init; }
    public double MutualMentorshipScore { get; init; }
    public double ThanksOccurrenceRate { get; init; }
    public double AverageThanksCoinCount { get; init; }
    public double ThanksChallengeRate { get; init; }
    public double ThanksBridgeRate { get; init; }
    public double ThanksConcentration { get; init; }
    public double ThanksToEmergenceContribution { get; init; }
    public double AverageEgoPenaltyApplied { get; init; }
    public double AverageHierarchyPenaltyApplied { get; init; }
    public int LearnedFromUnexpectedAgentCount { get; init; }
}

internal sealed record PhaseCountRow(string Phase, string Label, int Count);
