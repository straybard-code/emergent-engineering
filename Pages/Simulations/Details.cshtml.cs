using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Simulations;

public sealed class DetailsModel(
    AppDbContext db,
    ISimulationRunner runner,
    ExperimentCleanupService cleanupService,
    ParameterSweepRunner sweepRunner,
    ProductivityEvaluationService productivityEvaluationService) : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const double SvgCenterX = 350;
    private const double SvgCenterY = 220;
    private const double SvgRadius = 150;

    public string SelectedView { get; private set; } = "logs";
    public SimulationProject? Project { get; private set; }
    public Agent? SelectedAgent { get; private set; }
    public List<AgentAction> Actions { get; private set; } = [];
    public List<AgentAction> AgentHistory { get; private set; } = [];
    public List<PhasePoint> PhaseHistory { get; private set; } = [];
    public List<AgentTrustSummary> AgentTrustSummaries { get; private set; } = [];
    public List<TrustChangeLogRow> TrustChangeLogs { get; private set; } = [];
    public List<NetworkSnapshot> NetworkSnapshots { get; private set; } = [];
    public NetworkMetricsResult? FinalNetworkMetrics { get; private set; }
    public List<ThresholdSweepPoint> ThresholdSweep { get; private set; } = [];
    public List<ActionTimelinePoint> ActionTimeline { get; private set; } = [];
    public List<PhaseTransitionInsight> PhaseTransitionInsights { get; private set; } = [];
    public List<PrecursorPoint> PrecursorPoints { get; private set; } = [];
    public List<KnowledgeTimelinePoint> KnowledgeTimeline { get; private set; } = [];
    public KnowledgeTimelinePoint? FinalKnowledgePoint { get; private set; }
    public ProductivityEvaluation? ProductivityEvaluation { get; private set; }
    public double AverageThinkingSpeed { get; private set; }
    public double MinThinkingSpeed { get; private set; }
    public double MaxThinkingSpeed { get; private set; }
    public double ThinkingSpeedDispersionActual { get; private set; }
    public double GeneratedIdeaCountTotal { get; private set; }
    public double ProcessedIdeaCountTotal { get; private set; }
    public double FinalUnprocessedIdeaCount { get; private set; }
    public double GeneratedHypothesisCountTotal { get; private set; }
    public double ValidatedHypothesisCountTotal { get; private set; }
    public double FinalUnvalidatedHypothesisCount { get; private set; }
    public double FinalCognitiveLoad { get; private set; }
    public double MaxCognitiveLoad { get; private set; }
    public double AverageThinkingSpeedMismatch { get; private set; }
    public double MaxThinkingSpeedMismatch { get; private set; }
    public int MaxConsecutiveHighLoadSteps { get; private set; }
    public string ThinkingSpeedInterpretation { get; private set; } = "-";
    public ThinkingSpeedNetworkAnalysisResult ThinkingSpeedNetworkAnalysis { get; private set; } = new();
    public List<EmergentCriterionEntry> FinalEmergentCriteria { get; private set; } = [];
    public string FinalPhaseDecisionReasonDisplay { get; private set; } = "-";
    public EmergenceFingerprint? Fingerprint { get; private set; }
    public string MostCommonPhaseTransitionPattern { get; private set; } = "-";
    public int? SelectedAgentId { get; private set; }
    public bool IsCompleted => Project is not null && Project.CurrentStep >= Project.TotalSteps;
    [TempData]
    public string? SimulationMessage { get; set; }
    public int PhaseTransitionCount => PhaseTransitionInsights.Count;
    public int? FirstPhaseTransitionStep => PhaseTransitionInsights.Count == 0 ? null : PhaseTransitionInsights.Min(item => item.StepNo);
    public int? LastPhaseTransitionStep => PhaseTransitionInsights.Count == 0 ? null : PhaseTransitionInsights.Max(item => item.StepNo);

    public async Task<IActionResult> OnGetAsync(int id, int? agentId, string? view)
    {
        SelectedAgentId = agentId;
        SelectedView = string.Equals(view, "history", StringComparison.OrdinalIgnoreCase) ? "history" : "logs";
        await LoadAsync(id, agentId);
        return Page();
    }

    public async Task<IActionResult> OnPostRunOneAsync(int id)
    {
        await runner.RunOneStepAsync(id);
        return RedirectToPage(new { id, view = "logs" });
    }

    public async Task<IActionResult> OnPostRunAllAsync(int id)
    {
        await runner.RunAllAsync(id);
        return RedirectToPage(new { id, view = "logs" });
    }

    public async Task<IActionResult> OnPostMarkFailedAsync(int id)
    {
        var updated = await cleanupService.MarkSimulationAsFailedAsync(id);
        SimulationMessage = updated
            ? "Simulationを失敗扱いにしました。"
            : "Simulationが見つかりませんでした。";
        return RedirectToPage(new { id, view = "logs" });
    }

    public async Task<IActionResult> OnPostForceDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var result = await cleanupService.ForceDeleteSimulationAsync(
            id,
            async (sweepId, innerCancellationToken) =>
            {
                await sweepRunner.RecalculateSweepStatusAsync(sweepId, innerCancellationToken);
            },
            cancellationToken);

        SimulationMessage = result.DeletedSimulationProjects > 0
            ? result.ToJapaneseMessage()
            : "削除対象のSimulationがありませんでした。";
        return RedirectToPage("/Index");
    }

    private async Task LoadAsync(int id, int? agentId)
    {
        Project = await db.SimulationProjects
            .Include(project => project.Agents)
            .Include(project => project.Metrics)
            .FirstOrDefaultAsync(project => project.Id == id);

        if (Project is null)
        {
            return;
        }

        var orderedActions = await db.AgentActions
            .Include(action => action.Agent)
            .Where(action => action.SimulationProjectId == id)
            .OrderBy(action => action.StepNo)
            .ThenBy(action => action.Id)
            .ToListAsync();

        Actions = orderedActions
            .OrderByDescending(action => action.StepNo)
            .ThenBy(action => action.AgentId)
            .ToList();

        var simulationSteps = await db.SimulationSteps
            .Where(step => step.SimulationProjectId == id)
            .OrderBy(step => step.StepNo)
            .ToListAsync();

        PhaseHistory = simulationSteps
            .Select(step => new PhasePoint
            {
                StepNo = step.StepNo,
                Phase = step.Phase,
                PhaseValue = MapPhase(step.Phase)
            })
            .ToList();

        KnowledgeTimeline = KnowledgeAnalysisService.BuildTimeline(simulationSteps);
        FinalKnowledgePoint = KnowledgeTimeline.OrderBy(point => point.StepNo).LastOrDefault();
        FinalEmergentCriteria = EmergentCriteriaParser.Parse(FinalKnowledgePoint?.EmergentCriteriaJson);
        FinalPhaseDecisionReasonDisplay = DescribePhaseDecisionReason(FinalKnowledgePoint);
        ProductivityEvaluation = BuildProductivityEvaluation();

        var phaseByStep = PhaseHistory.ToDictionary(point => point.StepNo, point => point.Phase);
        var trustSnapshots = await db.TrustSnapshots
            .Where(snapshot => snapshot.SimulationProjectId == id)
            .OrderBy(snapshot => snapshot.StepNo)
            .ThenBy(snapshot => snapshot.SourceAgentId)
            .ThenBy(snapshot => snapshot.TargetAgentId)
            .ToListAsync();
        var finalTrustRows = GetFinalTrustRows(Project, trustSnapshots);
        var actionRecords = orderedActions
            .Select(action => new PhaseTransitionActionRecord
            {
                StepNo = action.StepNo,
                AgentName = action.Agent?.Name ?? "",
                Action = action.Action,
                TargetAgentName = string.IsNullOrWhiteSpace(action.TargetAgentName) ? "-" : action.TargetAgentName,
                TrustBefore = action.TrustBefore,
                TrustDelta = action.TrustDelta,
                TrustAfter = action.TrustAfter
            })
            .ToList();
        NetworkSnapshots = BuildNetworkSnapshots(
            Project,
            simulationSteps,
            phaseByStep,
            NetworkMetricsCalculator.NormalizeThreshold(Project.EffectiveTrustThreshold));
        var edgeRowsByStep = BuildEdgeRowsByStep(NetworkSnapshots);
        FinalNetworkMetrics = CalculateNetworkMetrics(Project.Agents, finalTrustRows, Project.EffectiveTrustThreshold);
        BuildThinkingSpeedSummary();
        ThinkingSpeedNetworkAnalysis = ThinkingSpeedAnalysisService.BuildNetworkAnalysis(Project, FinalKnowledgePoint, FinalNetworkMetrics);
        ThresholdSweep = BuildThresholdSweep(Project.Agents, finalTrustRows);
        ActionTimeline = BuildActionTimeline(orderedActions, phaseByStep, Project.Phase);
        var phaseTransitionStates = BuildPhaseTransitionStates(
            Project.CurrentStep,
            ActionTimeline,
            NetworkSnapshots,
            KnowledgeTimeline,
            edgeRowsByStep,
            Project.Agents.Count,
            Project.EffectiveTrustThreshold);
        PhaseTransitionInsights = BuildPhaseTransitionInsights(
            phaseTransitionStates,
            actionRecords,
            edgeRowsByStep,
            Project.EffectiveTrustThreshold);
        PrecursorPoints = PrecursorAnalysisService.BuildPoints(phaseTransitionStates, Project.PsychologicalSafetyLevel);
        MostCommonPhaseTransitionPattern = PhaseTransitionInspector.BuildMostCommonPatternLabel(PhaseTransitionInsights);
        Fingerprint = FinalNetworkMetrics is null
            ? null
            : EmergenceFingerprintService.Build(
                Project,
                Project.Name,
                Project.Id,
                orderedActions,
                KnowledgeTimeline,
                PhaseTransitionInsights,
                ThresholdSweep,
                FinalNetworkMetrics);

        AgentTrustSummaries = Project.Agents
            .OrderBy(agent => agent.Id)
            .Select(agent => new AgentTrustSummary
            {
                AgentName = agent.Name,
                Role = agent.Role,
                Personality = agent.Personality,
                Orientation = agent.Orientation,
                AverageTrust = TrustJsonUtility.CalculateAverage(agent.TrustJson),
                TrustJson = agent.TrustJson
            })
            .ToList();

        TrustChangeLogs = Actions
            .Select(action => new TrustChangeLogRow
            {
                StepNo = action.StepNo,
                AgentName = action.Agent?.Name ?? "",
                Action = action.Action,
                TrustBefore = action.TrustBefore,
                TrustDelta = action.TrustDelta,
                TrustAfter = action.TrustAfter,
                Phase = phaseByStep.GetValueOrDefault(action.StepNo, SimulationPhase.Forming)
            })
            .ToList();

        if (agentId.HasValue)
        {
            SelectedAgent = Project.Agents.FirstOrDefault(agent => agent.Id == agentId.Value);
            Actions = Actions
                .Where(action => action.AgentId == agentId.Value)
                .ToList();

            TrustChangeLogs = TrustChangeLogs
                .Where(log => string.Equals(log.AgentName, SelectedAgent?.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            AgentHistory = Actions
                .Where(action => action.AgentId == agentId.Value)
                .OrderBy(action => action.StepNo)
                .ToList();
        }
    }

    public string GetActionCssClass(string action)
    {
        return action switch
        {
            AgentActionType.ShareInfo => "action-share",
            AgentActionType.AskHelp => "action-help",
            AgentActionType.ProposeIdea => "action-propose",
            AgentActionType.Criticize => "action-criticize",
            AgentActionType.WorkAlone => "action-alone",
            AgentActionType.SupportOther => "action-support",
            _ => "action-wait"
        };
    }

    public string GetPhaseHistoryJson()
    {
        return JsonSerializer.Serialize(PhaseHistory, JsonOptions);
    }

    public string GetNetworkSnapshotsJson()
    {
        return JsonSerializer.Serialize(NetworkSnapshots, JsonOptions);
    }

    public string GetNetworkSnapshotsByStepJson()
    {
        var snapshotsByStep = NetworkSnapshots
            .GroupBy(snapshot => snapshot.StepNo)
            .ToDictionary(group => group.Key, group => group.First());

        return JsonSerializer.Serialize(snapshotsByStep, JsonOptions);
    }

    public string GetThresholdSweepJson()
    {
        return JsonSerializer.Serialize(ThresholdSweep, JsonOptions);
    }

    public string GetActionTimelineJson()
    {
        return JsonSerializer.Serialize(ActionTimeline, JsonOptions);
    }

    public string GetPrecursorPointsJson()
    {
        return JsonSerializer.Serialize(PrecursorPoints, JsonOptions);
    }

    public string GetKnowledgeTimelineJson()
    {
        return JsonSerializer.Serialize(KnowledgeTimeline, JsonOptions);
    }

    private ProductivityEvaluation? BuildProductivityEvaluation()
    {
        if (Project is null || KnowledgeTimeline.Count == 0)
        {
            return null;
        }

        double? emergentRate = KnowledgeTimeline.Count == 0
            ? null
            : KnowledgeTimeline.Count(point => string.Equals(point.SelectedPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase)) / (double)KnowledgeTimeline.Count;

        var managementIndicators = new ProductivityIndicators
        {
            PhaseStability = Project.Metrics?.PhaseStability,
            PipelineCompletionScore = FinalKnowledgePoint?.PipelineCompletionScore,
            EffectiveDensity = FinalNetworkMetrics?.EffectiveNetworkDensity,
            ChaosRate = KnowledgeTimeline.Average(point => point.ChaosScore),
            CollapseRate = KnowledgeTimeline.Average(point => point.CollapseScore),
            SiloRate = KnowledgeTimeline.Average(point => point.SiloScore)
        };

        var emergenceIndicators = new ProductivityIndicators
        {
            KnowledgeReconfigurationScore = KnowledgeTimeline.Average(point => point.KnowledgeReconfigurationScore),
            KnowledgeRecombinationScore = KnowledgeTimeline.Average(point => point.KnowledgeRecombinationScore),
            SerendipityScore = KnowledgeTimeline.Average(point => point.SerendipityScore),
            ExplorationScore = KnowledgeTimeline.Average(point => point.ExplorationScore),
            CrossDomainExposure = KnowledgeTimeline.Average(point => point.CrossDomainExposure),
            ChallengeAcceptanceScore = KnowledgeTimeline.Average(point => point.ChallengeAcceptanceScore),
            EmergentRate = emergentRate
        };

        var institutionalizationIndicators = new ProductivityIndicators
        {
            LearningScore = KnowledgeTimeline.Average(point => point.LearningScore),
            AdaptationScore = KnowledgeTimeline.Average(point => point.AdaptationScore),
            PipelineCompletionScore = KnowledgeTimeline.Average(point => point.PipelineCompletionScore),
            AverageTrust = FinalNetworkMetrics?.AverageTrust,
            EffectiveDensity = FinalNetworkMetrics?.EffectiveNetworkDensity,
            AverageRespect = KnowledgeTimeline.Average(point => point.AverageRespect),
            AverageIntellectualRespect = KnowledgeTimeline.Average(point => point.AverageIntellectualRespect),
            MutualMentorshipScore = KnowledgeTimeline.Average(point => point.MutualMentorshipScore)
        };

        var management = productivityEvaluationService.Evaluate(managementIndicators).ManagementProductivityScore;
        var emergence = productivityEvaluationService.Evaluate(emergenceIndicators).EmergenceProductivityScore;
        var institutionalization = productivityEvaluationService.Evaluate(institutionalizationIndicators).InstitutionalizationProductivityScore;
        return productivityEvaluationService.CreateEvaluation(management, emergence, institutionalization);
    }

    private void BuildThinkingSpeedSummary()
    {
        if (Project is null || KnowledgeTimeline.Count == 0)
        {
            ThinkingSpeedInterpretation = "-";
            return;
        }

        var points = KnowledgeTimeline.ToList();
        var enabledPoints = points.Where(point => point.ThinkingSpeedModelEnabled).ToList();
        var sourcePoints = enabledPoints.Count > 0 ? enabledPoints : points;

        AverageThinkingSpeed = Math.Round(sourcePoints.Average(point => point.AverageThinkingSpeed), 4);
        MinThinkingSpeed = Math.Round(sourcePoints.Min(point => point.MinThinkingSpeed), 4);
        MaxThinkingSpeed = Math.Round(sourcePoints.Max(point => point.MaxThinkingSpeed), 4);
        ThinkingSpeedDispersionActual = Math.Round(sourcePoints.Average(point => point.ThinkingSpeedDispersionActual), 4);
        GeneratedIdeaCountTotal = Math.Round(points.Sum(point => point.GeneratedIdeaCount), 4);
        ProcessedIdeaCountTotal = Math.Round(points.Sum(point => point.ProcessedIdeaCount), 4);
        FinalUnprocessedIdeaCount = Math.Round(FinalKnowledgePoint?.UnprocessedIdeaCount ?? 0, 4);
        GeneratedHypothesisCountTotal = Math.Round(points.Sum(point => point.GeneratedHypothesisCount), 4);
        ValidatedHypothesisCountTotal = Math.Round(points.Sum(point => point.ValidatedHypothesisCount), 4);
        FinalUnvalidatedHypothesisCount = Math.Round(FinalKnowledgePoint?.UnvalidatedHypothesisCount ?? 0, 4);
        FinalCognitiveLoad = Math.Round(FinalKnowledgePoint?.CognitiveLoad ?? 0, 4);
        MaxCognitiveLoad = Math.Round(points.Max(point => point.CognitiveLoad), 4);
        AverageThinkingSpeedMismatch = Math.Round(points.Average(point => point.ThinkingSpeedMismatch), 4);
        MaxThinkingSpeedMismatch = Math.Round(points.Max(point => point.ThinkingSpeedMismatch), 4);
        MaxConsecutiveHighLoadSteps = points.Max(point => point.ConsecutiveHighLoadSteps);
        ThinkingSpeedInterpretation = BuildThinkingSpeedInterpretation();
    }

    private string BuildThinkingSpeedInterpretation()
    {
        if (Project is null || KnowledgeTimeline.Count == 0)
        {
            return "-";
        }

        var averageTrust = FinalNetworkMetrics?.AverageTrust ?? 0;
        var thinkingEnabled = Project.EnableThinkingSpeedModel;
        if (!thinkingEnabled)
        {
            return "思考速度モデルは無効です。設定値は保存されていますが、既存のシミュレーション挙動には影響していません。";
        }

        var decisionCoverage = AverageThinkingSpeed <= 0
            ? 0
            : Project.OrganizationalDecisionSpeed / Math.Max(AverageThinkingSpeed, 0.000001);
        var validationCoverage = AverageThinkingSpeed <= 0
            ? 0
            : Project.OrganizationalValidationSpeed / Math.Max(AverageThinkingSpeed, 0.000001);

        if (AverageThinkingSpeed > 1.0
            && FinalCognitiveLoad <= 0.45
            && AverageThinkingSpeedMismatch <= 0.30
            && averageTrust >= 0.30
            && Project.PsychologicalSafetyLevel >= 0.35
            && Project.InformationSharingLevel >= 0.35
            && decisionCoverage >= 0.80
            && validationCoverage >= 0.80)
        {
            return "思考生成速度と組織の意思決定・検証速度が釣り合っています。高い思考速度が学習と創発へ変換されています。";
        }

        if (GeneratedIdeaCountTotal > ProcessedIdeaCountTotal)
        {
            return "提案生成速度が意思決定速度を上回り、未処理提案が蓄積しています。";
        }

        if (GeneratedHypothesisCountTotal > ValidatedHypothesisCountTotal)
        {
            return "仮説生成に対して検証速度が不足しています。";
        }

        if (MaxThinkingSpeedMismatch >= 0.45)
        {
            return "エージェント間の思考速度差が大きく、理解速度や説明負荷の差が摩擦につながっています。";
        }

        if (AverageThinkingSpeed > 1.0 && (averageTrust < 0.30 || Project.PsychologicalSafetyLevel < 0.35))
        {
            return "思考速度は高いものの、信頼と心理的安全性が不足しており、対立や分断が高速化する可能性があります。";
        }

        if (GeneratedIdeaCountTotal + GeneratedHypothesisCountTotal > ProcessedIdeaCountTotal + ValidatedHypothesisCountTotal)
        {
            return "提案・仮説の生成量が多く、共有・検証・統合の負荷が高まっています。";
        }

        return "思考生成速度と組織の処理能力は概ね釣り合っています。";
    }

    public string GetPipelineTimelineJson()
    {
        var points = KnowledgeTimeline.Select(point => new
        {
            stepNo = point.StepNo,
            serendipityScore = point.SerendipityScore,
            knowledgeRecombinationScore = point.KnowledgeRecombinationScore,
            knowledgeReconfigurationScore = point.KnowledgeReconfigurationScore,
            learningScore = point.LearningScore,
            adaptationScore = point.AdaptationScore,
            emergentScore = point.EmergentScore,
            pipelineCompletionScore = point.PipelineCompletionScore,
            pipelineBottleneck = point.PipelineBottleneck
        });

        return JsonSerializer.Serialize(points, JsonOptions);
    }

    public string GetPhaseScoreTimelineJson()
    {
        var points = KnowledgeTimeline.Select(point => new
        {
            stepNo = point.StepNo,
            emergentScore = point.EmergentScore,
            stableScore = point.StableScore,
            learningScore = point.LearningScore,
            siloScore = point.SiloScore,
            adaptationScore = point.AdaptationScore,
            chaosScore = point.ChaosScore,
            collapseScore = point.CollapseScore,
            selectedPhase = string.IsNullOrWhiteSpace(point.SelectedPhase) ? point.Phase : point.SelectedPhase
        });

        return JsonSerializer.Serialize(points, JsonOptions);
    }

    public string GetThanksCoinTimelineJson()
    {
        var points = KnowledgeTimeline.Select(point => new
        {
            stepNo = point.StepNo,
            thanksCoinOccurred = point.ThanksCoinOccurred,
            thanksCoinCount = point.ThanksCoinCount,
            thanksCoinHelpCount = point.ThanksCoinHelpCount,
            thanksCoinIdeaCount = point.ThanksCoinIdeaCount,
            thanksCoinChallengeCount = point.ThanksCoinChallengeCount,
            thanksCoinBridgeCount = point.ThanksCoinBridgeCount,
            averageRespect = point.AverageRespect,
            respectDensity = point.RespectDensity,
            respectConcentration = point.RespectConcentration,
            thanksConcentration = point.ThanksConcentration,
            thanksCoinTrustDelta = point.ThanksCoinTrustDelta,
            thanksCoinReconfigurationDelta = point.ThanksCoinReconfigurationDelta,
            thanksCoinSerendipityDelta = point.ThanksCoinSerendipityDelta,
            constructiveCriticismRate = point.ConstructiveCriticismRate,
            destructiveCriticismRate = point.DestructiveCriticismRate,
            challengeAcceptanceScore = point.ChallengeAcceptanceScore,
            respectReconfigurationBoost = point.RespectReconfigurationBoost,
            thanksChallengeEffect = point.ThanksChallengeEffect,
            thanksBridgeEffect = point.ThanksBridgeEffect,
            diversityRespectEffect = point.DiversityRespectEffect,
            respectEmergenceComponent = point.RespectEmergenceComponent,
            popularityTrapPenalty = point.PopularityTrapPenalty,
            popularityTrapDetected = point.PopularityTrapDetected,
            thanksCoinToReconfigurationContribution = point.ThanksCoinToReconfigurationContribution,
            thanksCoinToSerendipityContribution = point.ThanksCoinToSerendipityContribution,
            thanksCoinToEmergenceContribution = point.ThanksCoinToEmergenceContribution
        });

        return JsonSerializer.Serialize(points, JsonOptions);
    }

    public string FormatSignedDelta(double value, string format = "0.00")
    {
        return value.ToString($"+{format};-{format};0.00");
    }

    public string FormatTrustValue(double? value, bool includeSign = false)
    {
        if (!value.HasValue)
        {
            return "\u2014";
        }

        return includeSign ? value.Value.ToString("+0.00;-0.00;0.00") : value.Value.ToString("0.00");
    }

    public string GetTrustDeltaCssClass(double? value)
    {
        if (!value.HasValue)
        {
            return "trust-null";
        }

        if (value.Value > 0)
        {
            return "trust-positive";
        }

        if (value.Value < 0)
        {
            return "trust-negative";
        }

        return "trust-neutral";
    }

    public string GetCriterionValueText(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.000") : "--";
    }

    public string GetCriterionThresholdText(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.000") : "--";
    }

    public string GetCriterionRowClass(EmergentCriterionEntry row)
    {
        return row.Passed ? string.Empty : "table-danger";
    }

    public string GetPhaseDecisionReason(KnowledgeTimelinePoint point)
    {
        return DescribePhaseDecisionReason(point);
    }

    public string GetPipelineBottleneckDisplay(KnowledgeTimelinePoint? point)
    {
        return point is null || string.IsNullOrWhiteSpace(point.PipelineBottleneck)
            ? "--"
            : point.PipelineBottleneck;
    }

    public string GetPipelineBottleneckInterpretation(KnowledgeTimelinePoint? point)
    {
        return point is null
            ? "-"
            : KnowledgeAnalysisService.BuildPipelineBottleneckInterpretation(point.PipelineBottleneck);
    }

    private static int MapPhase(string phase)
    {
        return phase switch
        {
            SimulationPhase.Collapse => -3,
            SimulationPhase.Chaos => -2,
            SimulationPhase.Silo => -1,
            SimulationPhase.Adaptation => 2,
            SimulationPhase.Emergent => 4,
            SimulationPhase.Stable => 3,
            SimulationPhase.Learning => 1,
            SimulationPhase.Forming => 0,
            _ => 0
        };
    }

    private static string DescribePhaseDecisionReason(KnowledgeTimelinePoint? point)
    {
        if (point is null)
        {
            return "-";
        }

        var phase = string.IsNullOrWhiteSpace(point.SelectedPhase) ? point.Phase : point.SelectedPhase;
        return phase switch
        {
            SimulationPhase.Collapse => "待機と情報不足が支配的なため Collapse と判定しました。",
            SimulationPhase.Chaos => "批判優勢とネットワーク不安定化が強いため Chaos と判定しました。",
            SimulationPhase.Silo => "単独作業と共有不足が支配的なため Silo と判定しました。",
            SimulationPhase.Adaptation => "Challenge 後に探索と再構成が進んでいるため Adaptation と判定しました。",
            SimulationPhase.Emergent => $"EmergentScoreが {point.EmergentScore:0.000} のため創発期と判定しました。",
            SimulationPhase.Stable => "信頼と密度は高いが、知識再結合と探索が弱いため Stable と判定しました。",
            SimulationPhase.Learning => "知識共有と学習は進んでいますが、構造変化には届いていないため Learning と判定しました。",
            SimulationPhase.Forming => "初期形成段階に近いため Forming と判定しました。",
            _ => string.IsNullOrWhiteSpace(point.PhaseDecisionReason) ? "-" : point.PhaseDecisionReason
        };
    }

    private static List<NetworkSnapshot> BuildNetworkSnapshots(
        SimulationProject project,
        IReadOnlyCollection<SimulationStep> simulationSteps,
        IReadOnlyDictionary<int, string> phaseByStep,
        double effectiveTrustThreshold)
    {
        if (project.Agents.Count == 0 || project.CurrentStep <= 0)
        {
            return [];
        }

        if (simulationSteps.Count == 0)
        {
            return BuildFallbackNetworkSnapshots(project, phaseByStep, effectiveTrustThreshold);
        }

        return simulationSteps
            .OrderBy(step => step.StepNo)
            .Select(step => CreateSnapshotFromRows(
                step.StepNo,
                phaseByStep.GetValueOrDefault(step.StepNo, step.StepNo <= 2 ? SimulationPhase.Forming : project.Phase),
                project.Agents,
                effectiveTrustThreshold,
                CreateTrustRowsFromStateJson(step.StateJson, project.Agents)))
            .ToList();
    }

    private static List<NetworkSnapshot> BuildFallbackNetworkSnapshots(
        SimulationProject project,
        IReadOnlyDictionary<int, string> phaseByStep,
        double effectiveTrustThreshold)
    {
        var rows = CreateTrustRowsFromCurrentState(project.Agents);
        List<NetworkSnapshot> snapshots = [];

        for (var stepNo = 1; stepNo <= project.CurrentStep; stepNo++)
        {
            snapshots.Add(CreateSnapshotFromRows(
                stepNo,
                phaseByStep.GetValueOrDefault(stepNo, stepNo <= 2 ? SimulationPhase.Forming : project.Phase),
                project.Agents,
                effectiveTrustThreshold,
                rows));
        }

        return snapshots;
    }

    private static IReadOnlyDictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>> BuildEdgeRowsByStep(
        IReadOnlyCollection<NetworkSnapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return new Dictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>>();
        }

        return snapshots.ToDictionary(
            snapshot => snapshot.StepNo,
            snapshot => (IReadOnlyCollection<NetworkTrustEdgeRef>)snapshot.Edges
                .Select(edge => new NetworkTrustEdgeRef(
                    edge.SourceAgentId,
                    edge.TargetAgentId,
                    edge.SourceAgentName,
                    edge.TargetAgentName,
                    TrustJsonUtility.Clamp(edge.Trust)))
                .ToList());
    }

    private static List<TrustRow> CreateTrustRowsFromStateJson(
        string? stateJson,
        IReadOnlyCollection<Agent> fallbackAgents)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
        {
            return CreateTrustRowsFromCurrentState(fallbackAgents);
        }

        try
        {
            using var document = JsonDocument.Parse(stateJson);
            var root = document.RootElement;
            if (!TryGetPropertyIgnoreCase(root, "Agents", out var agentsElement) || agentsElement.ValueKind != JsonValueKind.Array)
            {
                return CreateTrustRowsFromCurrentState(fallbackAgents);
            }

            List<StepAgentTrustProjection> agents = [];
            foreach (var element in agentsElement.EnumerateArray())
            {
                var id = TryGetPropertyIgnoreCase(element, "Id", out var idElement) && idElement.TryGetInt32(out var parsedId)
                    ? parsedId
                    : -1;
                var name = TryGetPropertyIgnoreCase(element, "Name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                    ? nameElement.GetString() ?? ""
                    : "";
                var trustJson = TryGetPropertyIgnoreCase(element, "TrustJson", out var trustJsonElement) && trustJsonElement.ValueKind == JsonValueKind.String
                    ? trustJsonElement.GetString() ?? "{}"
                    : "{}";
                if (id <= 0 || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                agents.Add(new StepAgentTrustProjection(id, name, trustJson));
            }

            if (agents.Count == 0)
            {
                return CreateTrustRowsFromCurrentState(fallbackAgents);
            }

            var trustMaps = agents.ToDictionary(agent => agent.Id, agent => TrustJsonUtility.Deserialize(agent.TrustJson));
            List<TrustRow> rows = [];

            foreach (var sourceAgent in agents)
            {
                foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
                {
                    var trustMap = trustMaps[sourceAgent.Id];
                    var trustValue = trustMap.TryGetValue(targetAgent.Name, out var value) ? value : 0;
                    rows.Add(new TrustRow(
                        sourceAgent.Id,
                        targetAgent.Id,
                        sourceAgent.Name,
                        targetAgent.Name,
                        TrustJsonUtility.Clamp(trustValue)));
                }
            }

            return rows;
        }
        catch
        {
            return CreateTrustRowsFromCurrentState(fallbackAgents);
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static List<TrustRow> CreateTrustRowsFromCurrentState(IReadOnlyCollection<Agent> agents)
    {
        var trustMaps = agents.ToDictionary(agent => agent.Id, agent => TrustJsonUtility.Deserialize(agent.TrustJson));
        List<TrustRow> rows = [];

        foreach (var sourceAgent in agents)
        {
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                var trustMap = trustMaps[sourceAgent.Id];
                var trustValue = trustMap.TryGetValue(targetAgent.Name, out var value) ? value : 0;
                rows.Add(new TrustRow(
                    sourceAgent.Id,
                    targetAgent.Id,
                    sourceAgent.Name,
                    targetAgent.Name,
                    TrustJsonUtility.Clamp(trustValue)));
            }
        }

        return rows;
    }

    private static List<TrustRow> GetFinalTrustRows(
        SimulationProject project,
        IReadOnlyCollection<TrustSnapshot> trustSnapshots)
    {
        if (trustSnapshots.Count == 0)
        {
            return CreateTrustRowsFromCurrentState(project.Agents);
        }

        var finalStepNo = trustSnapshots
            .Where(snapshot => snapshot.StepNo <= project.CurrentStep)
            .Select(snapshot => (int?)snapshot.StepNo)
            .Max();

        if (!finalStepNo.HasValue)
        {
            return CreateTrustRowsFromCurrentState(project.Agents);
        }

        return trustSnapshots
            .Where(snapshot => snapshot.StepNo == finalStepNo.Value)
            .OrderBy(snapshot => snapshot.SourceAgentId)
            .ThenBy(snapshot => snapshot.TargetAgentId)
            .Select(snapshot => new TrustRow(
                snapshot.SourceAgentId,
                snapshot.TargetAgentId,
                snapshot.SourceAgentName,
                snapshot.TargetAgentName,
                TrustJsonUtility.Clamp(snapshot.TrustValue)))
            .ToList();
    }

    private static NetworkMetricsResult CalculateNetworkMetrics(
        IReadOnlyCollection<Agent> agents,
        IReadOnlyCollection<TrustRow> rows,
        double threshold)
    {
        return NetworkMetricsCalculator.Calculate(
            agents.OrderBy(agent => agent.Id)
                .Select(agent => new NetworkAgentRef(agent.Id, agent.Name))
                .ToList(),
            rows.Select(row => new NetworkTrustEdgeRef(
                    row.SourceAgentId,
                    row.TargetAgentId,
                    row.SourceAgentName,
                    row.TargetAgentName,
                    row.TrustValue))
                .ToList(),
            threshold);
    }

    private static List<ThresholdSweepPoint> BuildThresholdSweep(
        IReadOnlyCollection<Agent> agents,
        IReadOnlyCollection<TrustRow> rows)
    {
        return NetworkMetricsCalculator.StandardThresholdSweepValues
            .Select(threshold =>
            {
                var metrics = CalculateNetworkMetrics(agents, rows, threshold);
                return new ThresholdSweepPoint
                {
                    Threshold = threshold,
                    EffectiveNetworkDensity = metrics.EffectiveNetworkDensity,
                    StrongLinkCount = metrics.StrongLinkCount,
                    WeakLinkCount = metrics.WeakLinkCount,
                    ComponentCount = metrics.ComponentCount,
                    IsolatedCount = metrics.IsolatedCount
                };
            })
            .ToList();
    }

    private static List<ActionTimelinePoint> BuildActionTimeline(
        IReadOnlyCollection<AgentAction> actions,
        IReadOnlyDictionary<int, string> phaseByStep,
        string fallbackPhase)
    {
        var points = actions
            .GroupBy(action => action.StepNo)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var distribution = ActionDistributionCalculator.Calculate(group.Select(action => action.Action));
                return new ActionTimelinePoint
                {
                    StepNo = group.Key,
                    TotalCount = distribution.TotalCount,
                    ShareInfoCount = distribution.ShareInfoCount,
                    AskHelpCount = distribution.AskHelpCount,
                    ProposeIdeaCount = distribution.ProposeIdeaCount,
                    CriticizeCount = distribution.CriticizeCount,
                    WorkAloneCount = distribution.WorkAloneCount,
                    SupportOtherCount = distribution.SupportOtherCount,
                    WaitCount = distribution.WaitCount,
                    OtherCount = distribution.OtherCount,
                    ShareInfoRate = distribution.ShareInfoRate,
                    AskHelpRate = distribution.AskHelpRate,
                    ProposeIdeaRate = distribution.ProposeIdeaRate,
                    CriticizeRate = distribution.CriticizeRate,
                    WorkAloneRate = distribution.WorkAloneRate,
                    SupportOtherRate = distribution.SupportOtherRate,
                    WaitRate = distribution.WaitRate,
                    OtherRate = distribution.OtherRate,
                    Phase = phaseByStep.GetValueOrDefault(group.Key, fallbackPhase)
                };
            })
            .ToList();

        for (var index = 1; index < points.Count; index++)
        {
            points[index].PhaseChanged = !string.Equals(
                points[index - 1].Phase,
                points[index].Phase,
                StringComparison.OrdinalIgnoreCase);
        }

        return points;
    }

    private static List<PhaseTransitionStepState> BuildPhaseTransitionStates(
        int currentStep,
        IReadOnlyCollection<ActionTimelinePoint> actionTimeline,
        IReadOnlyCollection<NetworkSnapshot> networkSnapshots,
        IReadOnlyCollection<KnowledgeTimelinePoint> knowledgeTimeline,
        IReadOnlyDictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>> edgeRowsByStep,
        int agentCount,
        double effectiveTrustThreshold)
    {
        var actionByStep = actionTimeline.ToDictionary(point => point.StepNo);
        var networkByStep = networkSnapshots.ToDictionary(point => point.StepNo);
        var knowledgeByStep = knowledgeTimeline.ToDictionary(point => point.StepNo);
        List<PhaseTransitionStepState> states = [];
        var maxEdges = Math.Max(agentCount * Math.Max(agentCount - 1, 0), 1);

        for (var stepNo = 1; stepNo <= currentStep; stepNo++)
        {
            if (!networkByStep.TryGetValue(stepNo, out var network))
            {
                continue;
            }

            actionByStep.TryGetValue(stepNo, out var action);
            knowledgeByStep.TryGetValue(stepNo, out var knowledge);
            states.Add(new PhaseTransitionStepState
            {
                StepNo = stepNo,
                Phase = network.Phase,
                ShareInfoRate = action?.ShareInfoRate ?? 0,
                AskHelpRate = action?.AskHelpRate ?? 0,
                ProposeIdeaRate = action?.ProposeIdeaRate ?? 0,
                CriticizeRate = action?.CriticizeRate ?? 0,
                WorkAloneRate = action?.WorkAloneRate ?? 0,
                SupportOtherRate = action?.SupportOtherRate ?? 0,
                WaitRate = action?.WaitRate ?? 0,
                OtherRate = action?.OtherRate ?? 0,
                AverageTrust = network.AverageTrust,
                EffectiveNetworkDensity = network.EffectiveNetworkDensity,
                NewStrongLinkRate = CalculateNewStrongLinkRate(
                    edgeRowsByStep,
                    stepNo,
                    effectiveTrustThreshold,
                    maxEdges),
                StrongLinkCount = network.StrongLinkCount,
                WeakLinkCount = network.WeakLinkCount,
                ComponentCount = network.ComponentCount,
                IsolatedCount = network.IsolatedCount,
                ChallengeOccurred = knowledge?.ChallengeOccurred ?? false,
                ChallengeActive = knowledge?.ChallengeActive ?? false,
                ChallengeResolved = knowledge?.ChallengeResolved ?? false,
                ChallengeResolutionScore = knowledge?.ChallengeResolutionScore ?? 0,
                ChallengeGap = knowledge?.ChallengeGap ?? 0,
                KnowledgeReconfigurationScore = knowledge?.KnowledgeReconfigurationScore ?? 0,
                ExplorationScore = knowledge?.ExplorationScore ?? 0,
                SerendipityScore = knowledge?.SerendipityScore ?? 0,
                SerendipityOccurred = knowledge?.SerendipityOccurred ?? false,
                KnowledgeRecombinationScore = knowledge?.KnowledgeRecombinationScore ?? 0,
                SerendipityDrivenReconfiguration = knowledge?.SerendipityDrivenReconfiguration ?? false,
                SerendipityToEmergenceLink = knowledge?.SerendipityToEmergenceLink ?? false
            });
        }

        return states;
    }

    private static List<PhaseTransitionInsight> BuildPhaseTransitionInsights(
        IReadOnlyCollection<PhaseTransitionStepState> states,
        IReadOnlyCollection<PhaseTransitionActionRecord> actionRecords,
        IReadOnlyDictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>> edgeRowsByStep,
        double effectiveTrustThreshold)
    {
        return PhaseTransitionInspector.BuildInsights(
            states,
            actionRecords,
            edgeRowsByStep,
            windowSize: 3,
            effectiveTrustThreshold: effectiveTrustThreshold);
    }

    private static double CalculateNewStrongLinkRate(
        IReadOnlyDictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>> edgeRowsByStep,
        int stepNo,
        double effectiveTrustThreshold,
        int maxEdges)
    {
        if (stepNo <= 1
            || !edgeRowsByStep.TryGetValue(stepNo - 1, out var beforeRows)
            || !edgeRowsByStep.TryGetValue(stepNo, out var afterRows))
        {
            return 0;
        }

        var beforeMap = beforeRows.ToDictionary(row => (row.SourceAgentName, row.TargetAgentName), row => row.TrustValue);
        var newStrongLinks = afterRows.Count(row =>
        {
            beforeMap.TryGetValue((row.SourceAgentName, row.TargetAgentName), out var beforeValue);
            return beforeValue < effectiveTrustThreshold && row.TrustValue >= effectiveTrustThreshold;
        });

        return Math.Round(newStrongLinks / (double)maxEdges, 4);
    }

    private static NetworkSnapshot CreateSnapshotFromRows(
        int stepNo,
        string phase,
        IReadOnlyCollection<Agent> agents,
        double effectiveTrustThreshold,
        IReadOnlyCollection<TrustRow> rows)
    {
        var agentList = agents.OrderBy(agent => agent.Id).ToList();
        var metrics = CalculateNetworkMetrics(agentList, rows, effectiveTrustThreshold);
        List<NetworkEdge> edges = [];

        foreach (var row in rows)
        {
            var trust = TrustJsonUtility.Clamp(row.TrustValue);
            var category = NetworkMetricsCalculator.Classify(trust, effectiveTrustThreshold);
            switch (category)
            {
                case TrustLinkCategory.Strong:
                    edges.Add(new NetworkEdge
                    {
                        SourceAgentId = row.SourceAgentId,
                        TargetAgentId = row.TargetAgentId,
                        SourceAgentName = row.SourceAgentName,
                        TargetAgentName = row.TargetAgentName,
                        Trust = Math.Round(trust, 2),
                        StrokeWidth = Math.Round(1 + (Math.Clamp(Math.Abs(trust), 0, 1) * 6), 2),
                        StrokeColor = "#8bb8ef"
                    });
                    break;

                case TrustLinkCategory.Weak:
                    edges.Add(new NetworkEdge
                    {
                        SourceAgentId = row.SourceAgentId,
                        TargetAgentId = row.TargetAgentId,
                        SourceAgentName = row.SourceAgentName,
                        TargetAgentName = row.TargetAgentName,
                        Trust = Math.Round(trust, 2),
                        StrokeWidth = 1,
                        StrokeColor = "#c3c9d4"
                    });
                    break;

                case TrustLinkCategory.Negative:
                    edges.Add(new NetworkEdge
                    {
                        SourceAgentId = row.SourceAgentId,
                        TargetAgentId = row.TargetAgentId,
                        SourceAgentName = row.SourceAgentName,
                        TargetAgentName = row.TargetAgentName,
                        Trust = Math.Round(trust, 2),
                        StrokeWidth = 1,
                        StrokeColor = "#d06a6a"
                    });
                    break;
            }
        }

        var nodes = agentList
            .Select((agent, index) =>
            {
                var angle = agentList.Count == 1
                    ? 0
                    : (-Math.PI / 2) + ((Math.PI * 2 * index) / agentList.Count);

                return new NetworkNode
                {
                    AgentId = agent.Id,
                    AgentName = agent.Name,
                    Role = agent.Role,
                    Personality = agent.Personality,
                    Orientation = agent.Orientation,
                    X = Math.Round(SvgCenterX + (SvgRadius * Math.Cos(angle)), 2),
                    Y = Math.Round(SvgCenterY + (SvgRadius * Math.Sin(angle)), 2),
                    IsHub = metrics.HubAgentId == agent.Id && metrics.HubAgentName != "-"
                };
            })
            .ToList();

        return new NetworkSnapshot
        {
            StepNo = stepNo,
            Phase = phase,
            AverageTrust = metrics.AverageTrust,
            AverageAbsTrust = metrics.AverageAbsTrust,
            NetworkDensity = metrics.NetworkDensity,
            EffectiveNetworkDensity = metrics.EffectiveNetworkDensity,
            StrongLinkCount = metrics.StrongLinkCount,
            WeakLinkCount = metrics.WeakLinkCount,
            ComponentCount = metrics.ComponentCount,
            HubAgentName = metrics.HubAgentName,
            HubScore = metrics.HubScore,
            IsolatedCount = metrics.IsolatedCount,
            Nodes = nodes,
            Edges = edges
        };
    }

    private sealed record TrustRow(
        int SourceAgentId,
        int TargetAgentId,
        string SourceAgentName,
        string TargetAgentName,
        double TrustValue);

    private sealed record StepAgentTrustProjection(
        int Id,
        string Name,
        string TrustJson);
}
