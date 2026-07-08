using System.Globalization;
using System.Net;
using System.Text;
using EmergentEngineering.Models;
using EmergentEngineering.Pages.PhaseDiagrams;
using Microsoft.AspNetCore.Hosting;

namespace EmergentEngineering.Services;

public sealed class PhaseDiagramReportService(IWebHostEnvironment webHostEnvironment)
{
    public async Task<PhaseDiagramReportResult> CreateReportAsync(
        EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model,
        CancellationToken cancellationToken = default)
    {
        if (model.Diagram is null)
        {
            throw new InvalidOperationException("Phase Diagram が見つかりませんでした。");
        }

        var generatedAt = DateTime.UtcNow;
        var html = BuildHtml(model, generatedAt);

        var reportDirectory = Path.Combine(GetWebRootPath(), "reports", "phase-diagrams");
        Directory.CreateDirectory(reportDirectory);

        var fileName = $"phase-diagram-{model.Diagram.Id}-{generatedAt:yyyyMMddHHmmss}.html";
        var reportPath = Path.Combine(reportDirectory, fileName);
        await File.WriteAllTextAsync(reportPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);

        return new PhaseDiagramReportResult(
            FileName: fileName,
            RelativeUrl: $"/reports/phase-diagrams/{fileName}",
            AbsolutePath: reportPath,
            CreatedAt: generatedAt);
    }

    private string BuildHtml(EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model, DateTime generatedAt)
    {
        var diagram = model.Diagram!;
        var metrics = BuildMetrics(model);
        var phaseDistribution = BuildPhaseDistribution(model.PointResults);
        var bottleneckDistribution = BuildBottleneckDistribution(model.PointResults);
        var learningRegion = BuildLearningRegionSummary(model.Points);
        var boundaryRegion = BuildBoundaryRegionSummary(model);
        var gridRows = BuildGridRows(model);
        var improvementDirection = BuildImprovementDirection(model, metrics, learningRegion);
        var interpretation = BuildInterpretation(model, metrics, learningRegion, boundaryRegion);

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"ja\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine($"<title>{Html(diagram.Name)} - Phase Diagram Report</title>");
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
            .container { max-width: 1600px; margin: 0 auto; }
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
            .grid-table td {
                min-width: 140px;
            }
            .cell-box {
                display: grid;
                gap: 2px;
            }
            .cell-phase {
                font-weight: 700;
            }
            .cell-metric {
                color: var(--muted);
                font-size: 0.85rem;
            }
            """);
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<div class=\"container\">");
        builder.AppendLine("<div class=\"header\">");
        builder.AppendLine("<div class=\"pill\">Phase Diagram Report</div>");
        builder.AppendLine($"<h1>{Html(diagram.Name)}</h1>");
        builder.AppendLine($"<p class=\"muted\">生成日時: {generatedAt:yyyy-MM-dd HH:mm:ss} UTC</p>");
        builder.AppendLine($"<p>{Html(diagram.Description)}</p>");
        builder.AppendLine("</div>");

        builder.AppendLine(BuildKeyValueSection("基本情報", new[]
        {
            ("相図名", diagram.Name),
            ("Scenario名", model.ScenarioName),
            ("X軸パラメータ", diagram.XParameterDisplayName),
            ("X範囲", $"{diagram.XStartValue:0.000} 〜 {diagram.XEndValue:0.000} / Step {diagram.XStepValue:0.000}"),
            ("Y軸パラメータ", diagram.YParameterDisplayName),
            ("Y範囲", $"{diagram.YStartValue:0.000} 〜 {diagram.YEndValue:0.000} / Step {diagram.YStepValue:0.000}"),
            ("グリッド数", model.TotalPointCount.ToString(CultureInfo.InvariantCulture)),
            ("完了セル数", model.CompletedPointCount.ToString(CultureInfo.InvariantCulture)),
            ("実行中セル数", model.RunningPointCount.ToString(CultureInfo.InvariantCulture)),
            ("未実行セル数", model.PendingPointCount.ToString(CultureInfo.InvariantCulture)),
            ("失敗セル数", model.FailedPointCount.ToString(CultureInfo.InvariantCulture)),
            ("進捗率", model.ProgressRate.ToString("P1", CultureInfo.InvariantCulture)),
            ("LLM", $"{diagram.LlmProvider} / {diagram.LlmModel}"),
            ("状態", FormatStatus(diagram.Status))
        }));

        builder.AppendLine(BuildMetricSection(metrics));
        builder.AppendLine(BuildRegionSummarySection("創発領域", model.EmergentRegionSummary));
        builder.AppendLine(BuildRegionSummarySection("学習領域", learningRegion));
        builder.AppendLine(BuildBoundarySection(model, boundaryRegion));
        builder.AppendLine(BuildPhaseDistributionSection(phaseDistribution));
        builder.AppendLine(BuildBottleneckDistributionSection(bottleneckDistribution));
        builder.AppendLine(BuildGridSection(model, gridRows));
        builder.AppendLine(BuildPointResultsSection(model));
        builder.AppendLine(BuildInterpretationSection("推奨改善方向", improvementDirection));
        builder.AppendLine(BuildInterpretationSection("解釈コメント", interpretation));

        builder.AppendLine("<div class=\"report-footer\">");
        builder.AppendLine("この HTML レポートは静的ファイルとして保存されるため、元の Phase Diagram を削除した後も閲覧できます。");
        builder.AppendLine("</div>");

        builder.AppendLine("</div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string BuildMetricSection(PhaseDiagramReportMetrics metrics)
    {
        var cards = new List<(string Label, string Value)>
        {
            ("平均信頼度", FormatDouble(metrics.AverageTrust)),
            ("実効密度", FormatDouble(metrics.AverageEffectiveDensity)),
            ("平均PipelineCompletion", FormatDouble(metrics.AveragePipelineCompletion)),
            ("平均KnowledgeReconfiguration", FormatDouble(metrics.AverageKnowledgeReconfiguration)),
            ("平均Serendipity", FormatDouble(metrics.AverageSerendipityRate)),
            ("平均相互敬意", FormatDouble(metrics.AverageRespect)),
            ("平均知的敬意", FormatDouble(metrics.AverageIntellectualRespect)),
            ("平均Challenge受容", FormatDouble(metrics.AverageChallengeAcceptanceScore)),
            ("平均Mutual Mentorship", FormatDouble(metrics.AverageMutualMentorshipScore)),
            ("Thanks→創発寄与", FormatDouble(metrics.AverageThanksCoinToEmergenceContribution)),
            ("Popularity Trap率", FormatDouble(metrics.PopularityTrapRate)),
            ("境界候補数", metrics.BoundaryCandidateCount.ToString(CultureInfo.InvariantCulture))
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

    private static string BuildRegionSummarySection(string title, PhaseDiagramRegionSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine($"<h2>{Html(title)}</h2>");
        if (!summary.HasData)
        {
            builder.AppendLine("<p class=\"muted\">この領域はまだ十分なデータがありません。</p>");
        }
        else
        {
            builder.AppendLine("<div class=\"cards\">");
            AppendCard(builder, "件数", summary.Count.ToString(CultureInfo.InvariantCulture));
            AppendCard(builder, "X範囲", FormatRange(summary.XMin, summary.XMax));
            AppendCard(builder, "Y範囲", FormatRange(summary.YMin, summary.YMax));
            AppendCard(builder, "中心", FormatCenter(summary.AverageX, summary.AverageY));
            AppendCard(builder, "平均PipelineCompletion", FormatNumber(summary.AveragePipelineCompletionScore));
            AppendCard(builder, "主なボトルネック", summary.MostCommonBottleneck);
            builder.AppendLine("</div>");
            builder.AppendLine("<div class=\"table-wrap mt-3\">");
            builder.AppendLine($"<p class=\"section-note\">{Html(summary.Interpretation)}</p>");
            builder.AppendLine("</div>");
        }
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildBoundarySection(EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model, PhaseDiagramRegionSummary boundaryRegion)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>境界領域</h2>");
        builder.AppendLine("<p class=\"section-note\">相境界として検出されたセル群とその近傍です。</p>");
        builder.AppendLine("<div class=\"cards mb-3\">");
        AppendCard(builder, "境界候補数", model.BoundaryCandidateCount.ToString(CultureInfo.InvariantCulture));
        AppendCard(builder, "Emergent境界数", model.EmergentBoundaryCount.ToString(CultureInfo.InvariantCulture));
        AppendCard(builder, "Stable境界数", model.StableBoundaryCount.ToString(CultureInfo.InvariantCulture));
        AppendCard(builder, "Silo境界数", model.SiloBoundaryCount.ToString(CultureInfo.InvariantCulture));
        AppendCard(builder, "最頻パターン", model.MostCommonBoundaryPattern);
        builder.AppendLine("</div>");
        if (boundaryRegion.HasData)
        {
            builder.AppendLine("<div class=\"cards mb-3\">");
            AppendCard(builder, "範囲", $"{FormatRange(boundaryRegion.XMin, boundaryRegion.XMax)} / {FormatRange(boundaryRegion.YMin, boundaryRegion.YMax)}");
            AppendCard(builder, "中心", FormatCenter(boundaryRegion.AverageX, boundaryRegion.AverageY));
            AppendCard(builder, "平均PipelineCompletion", FormatNumber(boundaryRegion.AveragePipelineCompletionScore));
            AppendCard(builder, "主なボトルネック", boundaryRegion.MostCommonBottleneck);
            builder.AppendLine("</div>");
            builder.AppendLine($"<p class=\"section-note\">{Html(boundaryRegion.Interpretation)}</p>");
        }

        if (model.BoundaryEntries.Count > 0)
        {
            builder.AppendLine("<div class=\"table-wrap\">");
            builder.AppendLine("<table>");
            builder.AppendLine("<thead><tr><th>From</th><th>To</th><th>X</th><th>Y</th><th>Neighbor X</th><th>Neighbor Y</th><th>Direction</th></tr></thead>");
            builder.AppendLine("<tbody>");
            foreach (var entry in model.BoundaryEntries)
            {
                builder.AppendLine("<tr>");
                builder.AppendLine($"<td>{Html(model.FormatBoundaryPhase(entry.FromPhase))}</td>");
                builder.AppendLine($"<td>{Html(model.FormatBoundaryPhase(entry.ToPhase))}</td>");
                builder.AppendLine($"<td>{entry.XValue:0.000}</td>");
                builder.AppendLine($"<td>{entry.YValue:0.000}</td>");
                builder.AppendLine($"<td>{entry.NeighborXValue:0.000}</td>");
                builder.AppendLine($"<td>{entry.NeighborYValue:0.000}</td>");
                builder.AppendLine($"<td>{Html(model.FormatDirection(entry.Direction))}</td>");
                builder.AppendLine("</tr>");
            }
            builder.AppendLine("</tbody>");
            builder.AppendLine("</table>");
            builder.AppendLine("</div>");
        }
        else
        {
            builder.AppendLine("<p class=\"muted mb-0\">境界領域はまだ見つかっていません。</p>");
        }
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildPhaseDistributionSection(IReadOnlyList<PhaseDistributionRow> rows)
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
            builder.AppendLine($"<td>{Html(row.Phase)}</td>");
            builder.AppendLine($"<td>{row.Count}</td>");
            builder.AppendLine($"<td>{row.Rate:0.000}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildBottleneckDistributionSection(IReadOnlyList<BottleneckDistributionRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>ボトルネック分布</h2>");
        builder.AppendLine("<div class=\"table-wrap\">");
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>ボトルネック</th><th>件数</th><th>比率</th></tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var row in rows)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{Html(row.Bottleneck)}</td>");
            builder.AppendLine($"<td>{row.Count}</td>");
            builder.AppendLine($"<td>{row.Rate:0.000}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildGridSection(EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model, IReadOnlyList<GridCellReportRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>グリッド結果</h2>");
        builder.AppendLine("<p class=\"section-note\">各 X / Y の組み合わせごとのセル結果です。</p>");
        builder.AppendLine("<div class=\"table-wrap\">");
        builder.AppendLine("<table class=\"grid-table\">");
        builder.AppendLine("<thead><tr><th>Y \\ X</th>");
        foreach (var x in model.XValues)
        {
            builder.AppendLine($"<th>{x:0.000}</th>");
        }
        builder.AppendLine("</tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var y in model.YValues.OrderByDescending(item => item))
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<th>{y:0.000}</th>");
            foreach (var x in model.XValues)
            {
                var cell = rows.FirstOrDefault(item => Math.Abs(item.XValue - x) < 0.0005 && Math.Abs(item.YValue - y) < 0.0005);
                if (cell is null)
                {
                    builder.AppendLine("<td>--</td>");
                    continue;
                }

                builder.AppendLine("<td>");
                builder.AppendLine("<div class=\"cell-box\">");
                builder.AppendLine($"<div class=\"cell-phase\">{Html(cell.DominantPhase)}</div>");
                builder.AppendLine($"<div class=\"cell-metric\">Emergent {cell.EmergentRate:0.00} / Stable {cell.StableRate:0.00} / Learning {cell.LearningRate:0.00}</div>");
                builder.AppendLine($"<div class=\"cell-metric\">Trust {cell.AverageTrust:0.00} / Density {cell.AverageEffectiveDensity:0.00}</div>");
                builder.AppendLine($"<div class=\"cell-metric\">Pipeline {cell.AveragePipelineCompletionScore:0.00} / Bottleneck {Html(cell.DominantBottleneck)}</div>");
                builder.AppendLine("</div>");
                builder.AppendLine("</td>");
            }
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildPointResultsSection(EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine("<h2>セルの詳細一覧</h2>");
        builder.AppendLine("<div class=\"table-wrap\">");
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>X</th><th>Y</th><th>Experiment</th><th>Phase</th><th>EmergentRate</th><th>StableRate</th><th>LearningRate</th><th>SiloRate</th><th>Trust</th><th>EffectiveDensity</th><th>KnowledgeReconfiguration</th><th>Serendipity</th><th>PipelineCompletion</th><th>ボトルネック</th></tr></thead>");
        builder.AppendLine("<tbody>");
        foreach (var row in model.PointResults)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td>{row.XValue:0.000}</td>");
            builder.AppendLine($"<td>{row.YValue:0.000}</td>");
            builder.AppendLine($"<td>{Html(FormatExperiment(model, row.ExperimentId))}</td>");
            builder.AppendLine($"<td>{Html(model.FormatPhaseDisplay(row.DominantPhase))}</td>");
            builder.AppendLine($"<td>{row.EmergentRate:0.00}</td>");
            builder.AppendLine($"<td>{row.StableRate:0.00}</td>");
            builder.AppendLine($"<td>{row.LearningRate:0.00}</td>");
            builder.AppendLine($"<td>{row.SiloRate:0.00}</td>");
            builder.AppendLine($"<td>{row.AverageTrust:0.00}</td>");
            builder.AppendLine($"<td>{row.AverageEffectiveDensity:0.00}</td>");
            builder.AppendLine($"<td>{row.AverageKnowledgeReconfigurationScore:0.00}</td>");
            builder.AppendLine($"<td>{row.AverageSerendipityRate:0.00}</td>");
            builder.AppendLine($"<td>{row.AveragePipelineCompletionScore:0.00}</td>");
            builder.AppendLine($"<td>{Html(row.DominantBottleneck)}</td>");
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</tbody>");
        builder.AppendLine("</table>");
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildInterpretationSection(string title, string body)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<section class=\"section\">");
        builder.AppendLine($"<h2>{Html(title)}</h2>");
        builder.AppendLine($"<p>{Html(body)}</p>");
        builder.AppendLine("</section>");
        return builder.ToString();
    }

    private static string BuildImprovementDirection(
        EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model,
        PhaseDiagramReportMetrics metrics,
        PhaseDiagramRegionSummary learningRegion)
    {
        if (model.EmergentRegionSummary.HasData)
        {
            return "創発領域が見つかっています。境界周辺と高PipelineCompletion領域を重点的に再探索してください。";
        }

        if (metrics.AverageRespect < 0.40)
        {
            return "相互敬意がまだ十分ではありません。Thanks Coin と Mutual Respect の連鎖を強めると、創発への接続が見えやすくなります。";
        }

        if (metrics.AverageChallengeAcceptanceScore < 0.55)
        {
            return "建設的な異論受容が弱い状態です。心理的安全性と知的敬意のつながりを見直してください。";
        }

        if (metrics.AverageIntellectualRespect < 0.40)
        {
            return "知的敬意が不足しています。異分野接触と Mutual Mentorship を強めると、知識再構成が進みやすくなります。";
        }

        if (metrics.AveragePipelineCompletion >= 0.70 || learningRegion.Count > 0)
        {
            return "創発直前の条件が見えています。学習領域と境界領域の接点を細かく確認してください。";
        }

        if (metrics.AverageEffectiveDensity < 0.30)
        {
            return "実効ネットワーク密度が不足しています。信頼形成と接続構造の改善から始めるとよさそうです。";
        }

        return "学習領域、境界領域、創発領域の比較を続けると、次の探索方向が見えてきます。";
    }

    private static string BuildInterpretation(
        EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model,
        PhaseDiagramReportMetrics metrics,
        PhaseDiagramRegionSummary learningRegion,
        PhaseDiagramRegionSummary boundaryRegion)
    {
        var comments = new List<string>();

        if (metrics.MaxEmergentRate > 0)
        {
            comments.Add("いくつかのセルで創発相に到達しています。境界付近の条件を詰めると、創発セルの面積が広がる可能性があります。");
        }
        else if (metrics.AveragePipelineCompletion >= 0.70)
        {
            comments.Add("相図全体としては創発直前まで進んでいます。PipelineCompletion が高い領域の近傍を追加探索してください。");
        }

        if (model.EmergentRegionSummary.HasData)
        {
            comments.Add("創発領域はすでに確認されています。境界領域の厚みと周辺のボトルネックを比較すると、移行条件が見えやすくなります。");
        }

        if (learningRegion.HasData)
        {
            comments.Add("学習領域は残っていますが、知識再構成やセレンディピティの連鎖がまだ創発閾値まで届いていない可能性があります。");
        }

        if (boundaryRegion.HasData)
        {
            comments.Add("境界領域では、Phase の切り替わりに合わせてボトルネックが変化しています。ここが再探索の主戦場です。");
        }

        if (metrics.AverageRespect < 0.40)
        {
            comments.Add("相互敬意が低いと、異論受容と知識再構成が伸びにくくなります。Thanks Coin の観測分布も確認してください。");
        }

        if (metrics.PopularityTrapRate >= 0.40)
        {
            comments.Add("人気投票停滞の兆候があります。Challenge / Bridge 型の Thanks が拾われているかを確認してください。");
        }

        if (comments.Count == 0)
        {
            comments.Add("相図の内部に学習領域と境界領域はありますが、創発に直結する閾値はまだ明確ではありません。");
        }

        return string.Join(" ", comments);
    }

    private static PhaseDiagramReportMetrics BuildMetrics(EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model)
    {
        var points = model.Points;
        return new PhaseDiagramReportMetrics
        {
            AverageTrust = points.Count == 0 ? 0 : Math.Round(points.Average(item => item.AverageTrust), 3),
            AverageEffectiveDensity = points.Count == 0 ? 0 : Math.Round(points.Average(item => item.AverageEffectiveDensity), 3),
            AveragePipelineCompletion = points.Count == 0 ? 0 : Math.Round(points.Average(item => item.AveragePipelineCompletionScore), 3),
            AverageKnowledgeReconfiguration = points.Count == 0 ? 0 : Math.Round(points.Average(item => item.AverageKnowledgeReconfigurationScore), 3),
            AverageSerendipityRate = points.Count == 0 ? 0 : Math.Round(points.Average(item => item.AverageSerendipityRate), 3),
            MaxEmergentRate = points.Count == 0 ? 0 : points.Max(item => item.EmergentRate),
            AverageRespect = model.AverageRespectMean,
            AverageIntellectualRespect = model.AverageIntellectualRespectMean,
            AverageChallengeAcceptanceScore = model.AverageChallengeAcceptanceScoreMean,
            AverageMutualMentorshipScore = model.AverageMutualMentorshipScoreMean,
            AverageThanksCoinToEmergenceContribution = model.AverageThanksCoinToEmergenceContributionMean,
            PopularityTrapRate = model.PopularityTrapRateMean,
            BoundaryCandidateCount = model.BoundaryCandidateCount
        };
    }

    private static IReadOnlyList<PhaseDistributionRow> BuildPhaseDistribution(IReadOnlyList<PhaseDiagramPointResultRow> rows)
    {
        var total = rows.Count;
        return new[]
        {
            SimulationPhase.Emergent,
            SimulationPhase.Stable,
            SimulationPhase.Learning,
            SimulationPhase.Adaptation,
            SimulationPhase.Forming,
            SimulationPhase.Silo,
            SimulationPhase.Chaos,
            SimulationPhase.Collapse
        }
        .Select(phase =>
        {
            var count = rows.Count(row => string.Equals(row.DominantPhase, phase, StringComparison.OrdinalIgnoreCase));
            return new PhaseDistributionRow(phase, count, total == 0 ? 0 : Math.Round(count / (double)total, 3));
        })
        .ToList();
    }

    private static IReadOnlyList<BottleneckDistributionRow> BuildBottleneckDistribution(IReadOnlyList<PhaseDiagramPointResultRow> rows)
    {
        var total = rows.Count;
        return rows
            .GroupBy(row => string.IsNullOrWhiteSpace(row.DominantBottleneck) ? "--" : row.DominantBottleneck)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new BottleneckDistributionRow(group.Key, group.Count(), total == 0 ? 0 : Math.Round(group.Count() / (double)total, 3)))
            .ToList();
    }

    private static PhaseDiagramRegionSummary BuildLearningRegionSummary(IReadOnlyList<PhaseDiagramPoint> points)
    {
        var selected = points.Where(point => string.Equals(point.DominantPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase)).ToList();
        return BuildRegionSummary("学習領域", selected);
    }

    private static PhaseDiagramRegionSummary BuildBoundaryRegionSummary(EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model)
    {
        if (model.BoundaryEntries.Count == 0)
        {
            return new PhaseDiagramRegionSummary
            {
                Title = "境界領域",
                EmptyMessage = "境界領域はまだ見つかっていません。"
            };
        }

        var xValues = model.BoundaryEntries.SelectMany(entry => new[] { entry.XValue, entry.NeighborXValue }).ToList();
        var yValues = model.BoundaryEntries.SelectMany(entry => new[] { entry.YValue, entry.NeighborYValue }).ToList();

        return new PhaseDiagramRegionSummary
        {
            Title = "境界領域",
            Count = model.BoundaryEntries.Count,
            XMin = xValues.Min(),
            XMax = xValues.Max(),
            YMin = yValues.Min(),
            YMax = yValues.Max(),
            AverageX = xValues.Average(),
            AverageY = yValues.Average(),
            AveragePipelineCompletionScore = model.Points.Count == 0 ? 0 : Math.Round(model.Points.Where(point => model.BoundaryEntries.Any(entry => Math.Abs(entry.XValue - point.XValue) < 0.0005 && Math.Abs(entry.YValue - point.YValue) < 0.0005 || Math.Abs(entry.NeighborXValue - point.XValue) < 0.0005 && Math.Abs(entry.NeighborYValue - point.YValue) < 0.0005)).Select(point => point.AveragePipelineCompletionScore).DefaultIfEmpty(0).Average(), 3),
            MostCommonBottleneck = model.MostCommonBoundaryPattern,
            Interpretation = "境界領域では Phase の切り替わりが起きています。ここを重点的に再探索すると、創発への遷移条件が見つかりやすくなります。"
        };
    }

    private static PhaseDiagramRegionSummary BuildRegionSummary(string title, IReadOnlyCollection<PhaseDiagramPoint> points)
    {
        if (points.Count == 0)
        {
            return new PhaseDiagramRegionSummary
            {
                Title = title,
                EmptyMessage = $"{title} はまだ十分なデータがありません。"
            };
        }

        var pointList = points.ToList();
        return new PhaseDiagramRegionSummary
        {
            Title = title,
            Count = pointList.Count,
            XMin = pointList.Min(item => item.XValue),
            XMax = pointList.Max(item => item.XValue),
            YMin = pointList.Min(item => item.YValue),
            YMax = pointList.Max(item => item.YValue),
            AverageX = pointList.Average(item => item.XValue),
            AverageY = pointList.Average(item => item.YValue),
            AveragePipelineCompletionScore = pointList.Average(item => item.AveragePipelineCompletionScore),
            MostCommonBottleneck = pointList
                .GroupBy(item => string.IsNullOrWhiteSpace(item.DominantBottleneck) ? "--" : item.DominantBottleneck)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => group.Key)
                .FirstOrDefault() ?? "--",
            Interpretation = $"この領域では {title} の条件が集まっています。"
        };
    }

    private static IReadOnlyList<GridCellReportRow> BuildGridRows(EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model)
    {
        return model.PointResults
            .Select(point => new GridCellReportRow
            {
                XValue = point.XValue,
                YValue = point.YValue,
                ExperimentId = point.ExperimentId,
                ExperimentName = point.ExperimentId.HasValue
                    ? (model.ExperimentNames.GetValueOrDefault(point.ExperimentId.Value, $"Experiment #{point.ExperimentId.Value}") ?? "-")
                    : "-",
                DominantPhase = model.FormatPhaseDisplay(point.DominantPhase),
                EmergentRate = point.EmergentRate,
                StableRate = point.StableRate,
                LearningRate = point.LearningRate,
                SiloRate = point.SiloRate,
                AverageTrust = point.AverageTrust,
                AverageEffectiveDensity = point.AverageEffectiveDensity,
                AverageKnowledgeReconfigurationScore = point.AverageKnowledgeReconfigurationScore,
                AverageSerendipityRate = point.AverageSerendipityRate,
                AveragePipelineCompletionScore = point.AveragePipelineCompletionScore,
                DominantBottleneck = point.DominantBottleneck
            })
            .ToList();
    }

    private static string FormatExperiment(EmergentEngineering.Pages.PhaseDiagrams.DetailsModel model, int? experimentId)
    {
        if (!experimentId.HasValue)
        {
            return "-";
        }

        return model.ExperimentNames.TryGetValue(experimentId.Value, out var name)
            ? $"{name} (#{experimentId.Value})"
            : $"Experiment #{experimentId.Value}";
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
        PhaseDiagramStatus.Created => "作成済み",
        PhaseDiagramStatus.Running => "実行中",
        PhaseDiagramStatus.Completed => "完了",
        PhaseDiagramStatus.Failed => "失敗",
        _ => status ?? "-"
    };

    private static string FormatDouble(double value, int digits = 3) => value.ToString($"0.{new string('0', digits)}", CultureInfo.InvariantCulture);

    private static string FormatNumber(double? value) => value.HasValue ? value.Value.ToString("0.000", CultureInfo.InvariantCulture) : "--";

    private static string FormatCenter(double? x, double? y)
    {
        if (!x.HasValue || !y.HasValue)
        {
            return "--";
        }

        return $"X={x.Value:0.000}, Y={y.Value:0.000}";
    }

    private static string FormatRange(double? min, double? max)
    {
        if (!min.HasValue || !max.HasValue)
        {
            return "--";
        }

        return $"{min.Value:0.000} 〜 {max.Value:0.000}";
    }

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

public sealed record PhaseDiagramReportResult(
    string FileName,
    string RelativeUrl,
    string AbsolutePath,
    DateTime CreatedAt);

internal sealed class PhaseDiagramReportMetrics
{
    public double AverageTrust { get; init; }
    public double AverageEffectiveDensity { get; init; }
    public double AveragePipelineCompletion { get; init; }
    public double AverageKnowledgeReconfiguration { get; init; }
    public double AverageSerendipityRate { get; init; }
    public double MaxEmergentRate { get; init; }
    public double AverageRespect { get; init; }
    public double AverageIntellectualRespect { get; init; }
    public double AverageChallengeAcceptanceScore { get; init; }
    public double AverageMutualMentorshipScore { get; init; }
    public double AverageThanksCoinToEmergenceContribution { get; init; }
    public double PopularityTrapRate { get; init; }
    public int BoundaryCandidateCount { get; init; }
}

internal sealed class PhaseDistributionRow
{
    public PhaseDistributionRow(string phase, int count, double rate)
    {
        Phase = phase;
        Count = count;
        Rate = rate;
    }

    public string Phase { get; }
    public int Count { get; }
    public double Rate { get; }
}

internal sealed class BottleneckDistributionRow
{
    public BottleneckDistributionRow(string bottleneck, int count, double rate)
    {
        Bottleneck = bottleneck;
        Count = count;
        Rate = rate;
    }

    public string Bottleneck { get; }
    public int Count { get; }
    public double Rate { get; }
}

internal sealed class GridCellReportRow
{
    public double XValue { get; init; }
    public double YValue { get; init; }
    public int? ExperimentId { get; init; }
    public string ExperimentName { get; init; } = "-";
    public string DominantPhase { get; init; } = "-";
    public double EmergentRate { get; init; }
    public double StableRate { get; init; }
    public double LearningRate { get; init; }
    public double SiloRate { get; init; }
    public double AverageTrust { get; init; }
    public double AverageEffectiveDensity { get; init; }
    public double AverageKnowledgeReconfigurationScore { get; init; }
    public double AverageSerendipityRate { get; init; }
    public double AveragePipelineCompletionScore { get; init; }
    public string DominantBottleneck { get; init; } = "--";
}
