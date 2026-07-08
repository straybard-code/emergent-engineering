using System.Globalization;
using System.Net;
using System.Text;
using EmergentEngineering.Models;
using EmergentEngineering.Pages.ParameterSweeps;
using Microsoft.AspNetCore.Hosting;

namespace EmergentEngineering.Services;

public sealed class ParameterSweepReportService(IWebHostEnvironment webHostEnvironment)
{
    public async Task<ParameterSweepReportResult> CreateReportAsync(
        EmergentEngineering.Pages.ParameterSweeps.DetailsModel model,
        CancellationToken cancellationToken = default)
    {
        if (model.Sweep is null)
        {
            throw new InvalidOperationException("Parameter Sweep が見つかりませんでした。");
        }

        var generatedAt = DateTime.UtcNow;
        var html = BuildHtml(model, generatedAt);

        var reportDirectory = Path.Combine(GetWebRootPath(), "reports", "parameter-sweeps");
        Directory.CreateDirectory(reportDirectory);

        var fileName = $"parameter-sweep-{model.Sweep.Id}-{generatedAt:yyyyMMddHHmmss}.html";
        var reportPath = Path.Combine(reportDirectory, fileName);
        await File.WriteAllTextAsync(reportPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);

        return new ParameterSweepReportResult(
            FileName: fileName,
            RelativeUrl: $"/reports/parameter-sweeps/{fileName}",
            AbsolutePath: reportPath,
            CreatedAt: generatedAt);
    }

    private string BuildHtml(EmergentEngineering.Pages.ParameterSweeps.DetailsModel model, DateTime generatedAt)
    {
        var sweep = model.Sweep!;
        var metrics = BuildMetrics(model);
        var phaseDistributionRows = BuildPhaseDistributionRows(model);
        var pointRows = BuildPointRows(model);
        var interpretation = BuildInterpretation(model, metrics);

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"ja\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine($"<title>{Html(sweep.Name)} - Parameter Sweep Report</title>");
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
            .container { max-width: 1400px; margin: 0 auto; }
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
            .section-note {
                margin: 0 0 16px;
                color: var(--muted);
            }
            .report-footer {
                color: var(--muted);
                font-size: 0.9rem;
                margin-top: 24px;
            }
            .pill {
                display: inline-block;
                padding: 4px 10px;
                border-radius: 999px;
                background: var(--accent-soft);
                color: #1d4ed8;
                font-size: 0.85rem;
                font-weight: 700;
            }
            """);
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<div class=\"container\">");
        builder.AppendLine("<div class=\"header\">");
        builder.AppendLine("<div class=\"pill\">Parameter Sweep Report</div>");
        builder.AppendLine($"<h1>{Html(sweep.Name)}</h1>");
        builder.AppendLine($"<p class=\"muted\">生成日時: {generatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");
        builder.AppendLine($"<p>{Html(sweep.Description)}</p>");
        builder.AppendLine("</div>");

        builder.AppendLine(BuildKeyValueSection("基本情報", new[]
        {
            ("Sweep名", sweep.Name),
            ("Scenario名", model.ScenarioName),
            ("対象パラメータ", model.TargetParameterLabel),
            ("Sweep範囲", $"{sweep.StartValue:0.000} 〜 {sweep.EndValue:0.000} / Step {sweep.StepValue:0.000}"),
            ("各値の実行回数", sweep.RunCountPerValue.ToString(CultureInfo.InvariantCulture)),
            ("Agent数", sweep.AgentCount.ToString(CultureInfo.InvariantCulture)),
            ("総Step数", sweep.TotalSteps.ToString(CultureInfo.InvariantCulture)),
            ("LLM", $"{sweep.LlmProvider} / {sweep.LlmModel}"),
            ("状態", FormatStatus(sweep.Status))
        }));

        builder.AppendLine(BuildMetricSection(metrics));
        builder.AppendLine(BuildPhaseDistributionSection(phaseDistributionRows));
        builder.AppendLine(BuildPointResultsSection(pointRows));
        builder.AppendLine(BuildTransitionSection(model));
        builder.AppendLine(BuildBottleneckSection(model));
        builder.AppendLine(BuildInterpretationSection(interpretation));

        builder.AppendLine("<div class=\"report-footer\">");
        builder.AppendLine("この HTML レポートは静的ファイルとして保存されるため、元の Parameter Sweep を削除した後も閲覧できます。");
        builder.AppendLine("</div>");

        builder.AppendLine("</div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string BuildMetricSection(ParameterSweepReportMetrics metrics)
    {
        var cards = new List<(string Label, string Value)>
        {
            ("最大創発率", FormatDouble(metrics.MaxEmergentRate)),
            ("最大安定率", FormatDouble(metrics.MaxStableRate)),
            ("最大学習率", FormatDouble(metrics.MaxLearningRate)),
            ("最大平均信頼度", FormatDouble(metrics.MaxAverageTrust)),
            ("最大実効密度", FormatDouble(metrics.MaxEffectiveDensity)),
            ("最大セレンディピティ率", FormatDouble(metrics.MaxSerendipityRate)),
            ("最大知識再構成", FormatDouble(metrics.MaxKnowledgeReconfiguration)),
            ("平均相互敬意", FormatDouble(metrics.AverageRespect)),
            ("平均知的敬意", FormatDouble(metrics.AverageIntellectualRespect)),
            ("平均Challenge受容", FormatDouble(metrics.AverageChallengeAcceptanceScore)),
            ("平均Mutual Mentorship", FormatDouble(metrics.AverageMutualMentorshipScore)),
            ("Popularity Trap率", FormatDouble(metrics.PopularityTrapRate)),
            ("Popularity Trap率", FormatDouble(metrics.PopularityTrapRate))
        };

        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>解析サマリー</h2>");
        builder.AppendLine("<div class=\"cards\">");
        foreach (var (label, value) in cards)
        {
            AppendCard(builder, label, value);
        }
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildPhaseDistributionSection(IReadOnlyList<ParameterSweepPhaseRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>Phase分布</h2>");
        builder.AppendLine("<p class=\"section-note\">各 ParameterValue ごとの最終 Phase 分布です。</p>");
        builder.AppendLine("<div class=\"table-wrap\">");
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>ParameterValue</th><th>Forming</th><th>Learning</th><th>Stable</th><th>Emergent</th><th>Silo</th><th>Chaos</th><th>Collapse</th></tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var row in rows)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{row.ParameterValue:0.000}</td>");
            builder.AppendLine($"<td>{row.GetCount(SimulationPhase.Forming)}</td>");
            builder.AppendLine($"<td>{row.GetCount(SimulationPhase.Learning)}</td>");
            builder.AppendLine($"<td>{row.GetCount(SimulationPhase.Stable)}</td>");
            builder.AppendLine($"<td>{row.GetCount(SimulationPhase.Emergent)}</td>");
            builder.AppendLine($"<td>{row.GetCount(SimulationPhase.Silo)}</td>");
            builder.AppendLine($"<td>{row.GetCount(SimulationPhase.Chaos)}</td>");
            builder.AppendLine($"<td>{row.GetCount(SimulationPhase.Collapse)}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildPointResultsSection(IReadOnlyList<ParameterSweepPointReportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>各ポイントの結果一覧</h2>");
        builder.AppendLine("<p class=\"section-note\">各 ParameterValue における主要指標を一覧化しています。</p>");
        builder.AppendLine("<div class=\"table-wrap\">");
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>ParameterValue</th><th>AverageTrust</th><th>EffectiveDensity</th><th>EmergentRate</th><th>StableRate</th><th>LearningRate</th><th>SiloRate</th><th>SerendipityRate</th><th>KnowledgeReconfiguration</th><th>AverageRespect</th><th>AverageIntellectualRespect</th><th>ChallengeAcceptance</th><th>Mutual Mentorship</th><th>Thanks Concentration</th><th>Popularity Trap率</th><th>主なボトルネック</th></tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var row in rows)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{row.ParameterValue:0.000}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.AverageTrust)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.EffectiveDensity)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.EmergentRate)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.StableRate)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.LearningRate)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.SiloRate)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.SerendipityRate)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.AverageKnowledgeReconfigurationScore)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.AverageRespect)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.AverageIntellectualRespect)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.AverageChallengeAcceptanceScore)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.MutualMentorshipScore)}</td>");
            builder.AppendLine($"<td>{FormatDouble(row.PopularityTrapRate)}</td>");
            builder.AppendLine($"<td>{Html(row.MostCommonPipelineBottleneck)}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildTransitionSection(EmergentEngineering.Pages.ParameterSweeps.DetailsModel model)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>相転移候補</h2>");
        builder.AppendLine("<p class=\"section-note\">隣接する ParameterValue 間の差分から検出した相転移候補です。</p>");
        if (model.TransitionCandidateRows.Count == 0)
        {
            builder.AppendLine("<p class=\"muted\">まだ相転移候補はありません。</p>");
        }
        else
        {
            builder.AppendLine("<div class=\"table-wrap\">");
            builder.AppendLine("<table>");
            builder.AppendLine("<thead><tr><th>区間</th><th>種類</th><th>変化量</th><th>解釈</th></tr></thead>");
            builder.AppendLine("<tbody>");
            foreach (var row in model.TransitionCandidateRows)
            {
                builder.AppendLine("<tr>");
                builder.AppendLine($"<td>{Html(row.Interval)}</td>");
                builder.AppendLine($"<td>{Html(row.Type)}</td>");
                builder.AppendLine($"<td>{Html(row.DeltaText)} <span class=\"muted\">({Html(row.ImpactLabel)})</span></td>");
                builder.AppendLine($"<td>{Html(row.Interpretation)}</td>");
                builder.AppendLine("</tr>");
            }
            builder.AppendLine("</tbody>");
            builder.AppendLine("</table>");
            builder.AppendLine("</div>");
        }
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildBottleneckSection(EmergentEngineering.Pages.ParameterSweeps.DetailsModel model)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>ボトルネック</h2>");
        builder.AppendLine("<p class=\"section-note\">Emergent にならなかった理由を集計しています。</p>");
        if (model.BottleneckAnalysisRows.Count == 0)
        {
            builder.AppendLine("<p class=\"muted\">まだボトルネック分析はありません。</p>");
        }
        else
        {
            builder.AppendLine("<div class=\"table-wrap\">");
            builder.AppendLine("<table>");
            builder.AppendLine("<thead><tr><th>理由</th><th>件数</th><th>比率</th><th>解釈</th></tr></thead>");
            builder.AppendLine("<tbody>");
            foreach (var row in model.BottleneckAnalysisRows)
            {
                builder.AppendLine("<tr>");
                builder.AppendLine($"<td>{Html(row.Reason)}</td>");
                builder.AppendLine($"<td>{row.Count}</td>");
                builder.AppendLine($"<td>{row.Rate:0.000}</td>");
                builder.AppendLine($"<td>{Html(row.Interpretation)}</td>");
                builder.AppendLine("</tr>");
            }
            builder.AppendLine("</tbody>");
            builder.AppendLine("</table>");
            builder.AppendLine("</div>");
        }
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildInterpretationSection(string interpretation)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>解釈コメント</h2>");
        builder.AppendLine($"<p>{Html(interpretation)}</p>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildInterpretation(EmergentEngineering.Pages.ParameterSweeps.DetailsModel model, ParameterSweepReportMetrics metrics)
    {
        var comments = new List<string>();

        if (metrics.MaxEmergentRate > 0)
        {
            comments.Add("一部の条件で創発相が見つかっています。最大創発率の付近を中心に追加検証するとよさそうです。");
        }
        else if (metrics.MaxEmergentScore > 0 && metrics.MaxEffectiveDensity >= 0.70)
        {
            comments.Add("創発直前の状態まで進んでいます。実効密度と知識再構成の組み合わせをさらに詰めると、創発相に届く可能性があります。");
        }
        else if (metrics.MaxEffectiveDensity < 0.30)
        {
            comments.Add("実効ネットワーク密度が不足しています。信頼形成や接続構造の見直しが必要です。");
        }

        if (metrics.AverageRespect < 0.40)
        {
            comments.Add("相互敬意がまだ十分ではありません。Thanks Coin と Mutual Respect の連鎖を確認してください。");
        }

        if (metrics.AverageChallengeAcceptanceScore < 0.55)
        {
            comments.Add("建設的な異論を受け止める力が弱いです。心理的安全性と知的敬意のつながりを強める余地があります。");
        }

        if (metrics.PopularityTrapRate >= 0.40)
        {
            comments.Add("Thanks が人気投票化している可能性があります。Bridge / Challenge 型の評価を増やすと、異質な貢献が拾われやすくなります。");
        }

        if (metrics.MaxSerendipityRate > 0 && metrics.MaxKnowledgeReconfiguration < 0.40)
        {
            comments.Add("セレンディピティは見えていますが、知識再構成への接続がまだ弱いです。異分野接触と再配線感度を見直してください。");
        }

        if (comments.Count == 0)
        {
            comments.Add("信頼・知識再構成・セレンディピティのつながりはありますが、創発を押し上げる決定的な条件はまだ見えていません。");
        }

        var recommendedRegion = BuildRecommendedRegion(model.AnalysisPoints);
        comments.Add($"推奨探索領域: {recommendedRegion}");

        return string.Join(" ", comments);
    }

    private static string BuildRecommendedRegion(IReadOnlyList<ParameterSweepAnalysisPoint> points)
    {
        if (points.Count == 0)
        {
            return "--";
        }

        var maxEmergentRate = points.Max(item => item.EmergentRate);
        var maxEmergentScore = points.Max(item => item.AverageEmergentScore);
        var maxEffectiveDensity = points.Max(item => item.EffectiveDensity);
        var maxKnowledgeReconfiguration = points.Max(item => item.AverageKnowledgeReconfigurationScore);

        IEnumerable<ParameterSweepAnalysisPoint> selected;
        string reason;

        if (maxEmergentRate > 0)
        {
            selected = points.Where(item => item.EmergentRate >= maxEmergentRate * 0.8);
            reason = "創発率が高い領域";
        }
        else if (maxEmergentScore > 0)
        {
            selected = points.Where(item => item.AverageEmergentScore >= maxEmergentScore * 0.8);
            reason = "創発スコアが高い領域";
        }
        else
        {
            selected = points.Where(item =>
                item.EffectiveDensity >= maxEffectiveDensity * 0.85 &&
                item.AverageKnowledgeReconfigurationScore >= maxKnowledgeReconfiguration * 0.85);
            reason = "実効密度と知識再構成が高い領域";
        }

        var values = selected.Select(item => item.ParameterValue).ToList();
        if (values.Count == 0)
        {
            values = points
                .OrderByDescending(item => item.EmergentRate + item.AverageEmergentScore + item.EffectiveDensity)
                .Take(Math.Max(1, points.Count / 3))
                .Select(item => item.ParameterValue)
                .ToList();
            reason = "総合的に強い領域";
        }

        return $"{values.Min():0.000} 〜 {values.Max():0.000} / {reason}";
    }

    private static ParameterSweepReportMetrics BuildMetrics(EmergentEngineering.Pages.ParameterSweeps.DetailsModel model)
    {
        var points = model.AnalysisPoints;
        return new ParameterSweepReportMetrics
        {
            MaxEmergentRate = points.Count == 0 ? 0 : points.Max(item => item.EmergentRate),
            MaxStableRate = points.Count == 0 ? 0 : points.Max(item => item.StableRate),
            MaxLearningRate = points.Count == 0 ? 0 : points.Max(item => item.LearningRate),
            MaxAverageTrust = points.Count == 0 ? 0 : points.Max(item => item.AverageTrust),
            MaxEffectiveDensity = points.Count == 0 ? 0 : points.Max(item => item.EffectiveDensity),
            MaxSerendipityRate = points.Count == 0 ? 0 : points.Max(item => item.SerendipityRate),
            MaxKnowledgeReconfiguration = points.Count == 0 ? 0 : points.Max(item => item.AverageKnowledgeReconfigurationScore),
            AverageRespect = points.Count == 0 ? 0 : Math.Round(points.Average(item => item.AverageRespect), 3),
            AverageIntellectualRespect = points.Count == 0 ? 0 : Math.Round(points.Average(item => item.AverageIntellectualRespect), 3),
            AverageChallengeAcceptanceScore = points.Count == 0 ? 0 : Math.Round(points.Average(item => item.AverageChallengeAcceptanceScore), 3),
            AverageMutualMentorshipScore = points.Count == 0 ? 0 : Math.Round(points.Average(item => item.MutualMentorshipScore), 3),
            PopularityTrapRate = points.Count == 0 ? 0 : Math.Round(points.Count(item => item.PopularityTrapRate >= 0.40) / (double)points.Count, 3),
            MaxEmergentScore = points.Count == 0 ? 0 : points.Max(item => item.AverageEmergentScore)
        };
    }

    private static List<ParameterSweepPhaseRow> BuildPhaseDistributionRows(EmergentEngineering.Pages.ParameterSweeps.DetailsModel model)
    {
        return model.PhaseComparison
            .OrderBy(item => item.ParameterValue)
            .ToList();
    }

    private static List<ParameterSweepPointReportRow> BuildPointRows(EmergentEngineering.Pages.ParameterSweeps.DetailsModel model)
    {
        return model.AnalysisPoints
            .OrderBy(item => item.ParameterValue)
            .Select(item => new ParameterSweepPointReportRow
            {
                ParameterValue = item.ParameterValue,
                AverageTrust = item.AverageTrust,
                EffectiveDensity = item.EffectiveDensity,
                EmergentRate = item.EmergentRate,
                StableRate = item.StableRate,
                LearningRate = item.LearningRate,
                SiloRate = item.SiloRate,
                SerendipityRate = item.SerendipityRate,
                AverageKnowledgeReconfigurationScore = item.AverageKnowledgeReconfigurationScore,
                AverageRespect = item.AverageRespect,
                AverageIntellectualRespect = item.AverageIntellectualRespect,
                AverageChallengeAcceptanceScore = item.AverageChallengeAcceptanceScore,
                MutualMentorshipScore = item.MutualMentorshipScore,
                PopularityTrapRate = item.PopularityTrapRate,
                MostCommonPipelineBottleneck = item.MostCommonPipelineBottleneck
            })
            .ToList();
    }

    private static string BuildKeyValueSection(string title, IReadOnlyCollection<(string Label, string Value)> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine($"<h2>{Html(title)}</h2>");
        builder.AppendLine("<div class=\"table-wrap\">");
        builder.AppendLine("<table>");
        builder.AppendLine("<tbody>");
        foreach (var (label, value) in rows)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<th style=\"width: 280px;\">{Html(label)}</th>");
            builder.AppendLine($"<td>{Html(value)}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static void AppendCard(StringBuilder builder, string label, string value)
    {
        builder.AppendLine("<div class=\"card\">");
        builder.AppendLine($"<span class=\"label\">{Html(label)}</span>");
        builder.AppendLine($"<span class=\"value\">{Html(value)}</span>");
        builder.AppendLine("</div>");
    }

    private static string FormatStatus(string? status) => status switch
    {
        Models.ParameterSweepStatus.Created => "作成済み",
        Models.ParameterSweepStatus.Running => "実行中",
        Models.ParameterSweepStatus.Completed => "完了",
        Models.ParameterSweepStatus.Failed => "失敗",
        Models.ParameterSweepStatus.StopRequested => "停止要求中",
        Models.ParameterSweepStatus.Stopped => "停止済み",
        _ => status ?? "-"
    };

    private static string FormatDouble(double value, int digits = 3) => value.ToString($"0.{new string('0', digits)}", CultureInfo.InvariantCulture);

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private string GetWebRootPath()
    {
        if (!string.IsNullOrWhiteSpace(webHostEnvironment.WebRootPath))
        {
            return webHostEnvironment.WebRootPath;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }
}

public sealed record ParameterSweepReportResult(
    string FileName,
    string RelativeUrl,
    string AbsolutePath,
    DateTime CreatedAt);

internal sealed class ParameterSweepReportMetrics
{
    public double MaxEmergentRate { get; init; }
    public double MaxStableRate { get; init; }
    public double MaxLearningRate { get; init; }
    public double MaxAverageTrust { get; init; }
    public double MaxEffectiveDensity { get; init; }
    public double MaxSerendipityRate { get; init; }
    public double MaxKnowledgeReconfiguration { get; init; }
    public double AverageRespect { get; init; }
    public double AverageIntellectualRespect { get; init; }
    public double AverageChallengeAcceptanceScore { get; init; }
    public double AverageMutualMentorshipScore { get; init; }
    public double PopularityTrapRate { get; init; }
    public double MaxEmergentScore { get; init; }
}

internal sealed class ParameterSweepPointReportRow
{
    public double ParameterValue { get; init; }
    public double AverageTrust { get; init; }
    public double EffectiveDensity { get; init; }
    public double EmergentRate { get; init; }
    public double StableRate { get; init; }
    public double LearningRate { get; init; }
    public double SiloRate { get; init; }
    public double SerendipityRate { get; init; }
    public double AverageKnowledgeReconfigurationScore { get; init; }
    public double AverageRespect { get; init; }
    public double AverageIntellectualRespect { get; init; }
    public double AverageChallengeAcceptanceScore { get; init; }
    public double MutualMentorshipScore { get; init; }
    public double PopularityTrapRate { get; init; }
    public string MostCommonPipelineBottleneck { get; init; } = "--";
}
