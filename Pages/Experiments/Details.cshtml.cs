using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Experiments;

public sealed class DetailsModel(
    AppDbContext db,
    ExperimentCleanupService cleanupService,
    ParameterSweepRunner parameterSweepRunner) : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [TempData]
    public string? ExperimentMessage { get; set; }

    public Experiment? Experiment { get; private set; }
    public List<ExperimentRun> Runs { get; private set; } = [];
    public List<PhaseDistributionRow> PhaseDistribution { get; private set; } = [];
    public List<PhaseTransitionRow> PhaseTransitions { get; private set; } = [];
    public List<TrustTrendPoint> TrustTrend { get; private set; } = [];
    public List<TrustStatePoint> TrustStateTrend { get; private set; } = [];
    public List<ExperimentActionTimelinePoint> ActionTimelineSummary { get; private set; } = [];
    public List<PhaseActionDistributionPoint> PhaseActionDistribution { get; private set; } = [];
    public List<ExperimentPrecursorPoint> PrecursorSummary { get; private set; } = [];
    public List<PhaseTransitionPatternSummary> PhaseTransitionPatternSummaries { get; private set; } = [];
    public List<TriggerAgentRankingSummary> TriggerAgentRankings { get; private set; } = [];
    public List<EmergenceFingerprint> RunFingerprints { get; private set; } = [];
    public List<ExperimentThresholdSweepPoint> ThresholdSweepSummary { get; private set; } = [];
    public double AverageTrustOverall { get; private set; }
    public double AverageComponentCountOverall { get; private set; }
    public int CompletedSimulationCount { get; private set; }
    public int RunningSimulationCount { get; private set; }
    public int PendingSimulationCount { get; private set; }
    public int FailedSimulationCount { get; private set; }
    public double FingerprintAverageTrustMean { get; private set; }
    public double FingerprintEffectiveDensityMean { get; private set; }
    public double FingerprintPhaseTransitionCountMean { get; private set; }
    public double FingerprintPhaseStabilityMean { get; private set; }
    public double FingerprintShareInfoRateMean { get; private set; }
    public double FingerprintWorkAloneRateMean { get; private set; }
    public double FingerprintThresholdFragilityScoreMean { get; private set; }
    public double AverageTrustGrowthRateEffectiveMean { get; private set; }
    public double AverageTrustDecayAppliedMean { get; private set; }
    public double AverageTrustCapacityPenaltyMean { get; private set; }
    public double AverageStrongTrustConcentrationMean { get; private set; }
    public double AverageRespectMean { get; private set; }
    public double AverageRespectDensityMean { get; private set; }
    public double AverageRespectConcentrationMean { get; private set; }
    public double AverageThanksCoinRateMean { get; private set; }
    public double AverageThanksHelpRateMean { get; private set; }
    public double AverageThanksIdeaRateMean { get; private set; }
    public double AverageThanksChallengeRateMean { get; private set; }
    public double AverageThanksBridgeRateMean { get; private set; }
    public double AverageThanksConcentrationMean { get; private set; }
    public double AverageThanksDiversityIndexMean { get; private set; }
    public double AverageThanksToEmergenceContributionMean { get; private set; }
    public double AverageChallengeAcceptanceScoreMean { get; private set; }
    public double AverageRespectReconfigurationBoostMean { get; private set; }
    public double AverageRespectEmergenceComponentMean { get; private set; }
    public double AverageThanksCoinToReconfigurationContributionMean { get; private set; }
    public double AverageThanksCoinToSerendipityContributionMean { get; private set; }
    public double AverageThanksCoinToEmergenceContributionMean { get; private set; }
    public double PopularityTrapRunCount { get; private set; }
    public double EmergentChallengeThanksRateMean { get; private set; }
    public double LearningChallengeThanksRateMean { get; private set; }
    public double AverageKnowledgeStockMean { get; private set; }
    public double AverageKnowledgeDiversityMean { get; private set; }
    public double AverageKnowledgeRewiringScoreMean { get; private set; }
    public double AverageExplorationScoreMean { get; private set; }
    public double AverageSerendipityScoreMean { get; private set; }
    public double AverageKnowledgeRecombinationScoreMean { get; private set; }
    public double AverageKnowledgeReconfigurationScoreMean { get; private set; }
    public int SerendipityOccurredRunCount { get; private set; }
    public double SerendipityOccurredRate { get; private set; }
    public int SerendipityToEmergenceLinkRunCount { get; private set; }
    public double SerendipityToEmergenceRate { get; private set; }
    public int ChallengeResolvedRunCount { get; private set; }
    public double AverageChallengeResolutionScoreMean { get; private set; }
    public double AverageAdaptationDurationMean { get; private set; }
    public int ShockOccurredRunCount { get; private set; }
    public int EmergentReachedRunCount { get; private set; }
    public int TrustCapacityExceededRunCount { get; private set; }
    public int OvertrustedCompleteNetworkRunCount { get; private set; }
    public int SelectiveTrustNetworkRunCount { get; private set; }
    public int FragileTrustNetworkRunCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Experiment = await db.Experiments
            .Include(item => item.Scenario)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (Experiment is null)
        {
            return Page();
        }

        Runs = await db.ExperimentRuns
            .Include(run => run.SimulationProject)
            .Where(run => run.ExperimentId == id)
            .OrderBy(run => run.RunNo)
            .ToListAsync();

        var simulationProjects = await db.SimulationProjects
            .Where(item => item.ExperimentId == id)
            .ToListAsync();

        AverageTrustOverall = Runs.Count == 0 ? 0 : Math.Round(Runs.Average(run => run.AverageTrust), 2);
        AverageComponentCountOverall = Runs.Count == 0 ? 0 : Math.Round(Runs.Average(run => run.ComponentCount), 2);
        CompletedSimulationCount = simulationProjects.Count(item => string.Equals(item.Status, SimulationStatus.Completed, StringComparison.OrdinalIgnoreCase) || item.CurrentStep >= item.TotalSteps);
        RunningSimulationCount = simulationProjects.Count(item => string.Equals(item.Status, SimulationStatus.Running, StringComparison.OrdinalIgnoreCase) && item.CurrentStep < item.TotalSteps);
        PendingSimulationCount = simulationProjects.Count(item => string.Equals(item.Status, SimulationStatus.Created, StringComparison.OrdinalIgnoreCase));
        FailedSimulationCount = simulationProjects.Count(item => string.Equals(item.Status, SimulationStatus.Failed, StringComparison.OrdinalIgnoreCase));

        PhaseDistribution = GetPhaseDistribution(Runs);
        PhaseTransitions = await GetPhaseTransitionsAsync(id);
        TrustTrend = await GetTrustTrendAsync(id);
        TrustStateTrend = await GetTrustStateTrendAsync(id);
        ActionTimelineSummary = await GetActionTimelineSummaryAsync(id);
        PhaseActionDistribution = await GetPhaseActionDistributionAsync(id);
        PrecursorSummary = await GetPrecursorSummaryAsync(id);
        var phaseTransitionInsights = await GetPhaseTransitionInsightsAsync(id);
        PhaseTransitionPatternSummaries = PhaseTransitionInspector.BuildPatternSummaries(phaseTransitionInsights);
        TriggerAgentRankings = PhaseTransitionInspector.BuildTriggerAgentRankings(phaseTransitionInsights);
        RunFingerprints = await GetRunFingerprintsAsync(id);
        FingerprintAverageTrustMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageTrust), 3);
        FingerprintEffectiveDensityMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.EffectiveDensity), 3);
        FingerprintPhaseTransitionCountMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.PhaseTransitionCount), 3);
        FingerprintPhaseStabilityMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.PhaseStability), 3);
        FingerprintShareInfoRateMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ShareInfoRate), 3);
        FingerprintWorkAloneRateMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.WorkAloneRate), 3);
        FingerprintThresholdFragilityScoreMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ThresholdFragilityScore), 3);
        AverageTrustGrowthRateEffectiveMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageTrustGrowthRateEffective), 3);
        AverageTrustDecayAppliedMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageTrustDecayApplied), 3);
        AverageTrustCapacityPenaltyMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageTrustCapacityPenalty), 3);
        AverageStrongTrustConcentrationMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.StrongTrustConcentration), 3);
        AverageRespectMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageRespect), 3);
        AverageRespectDensityMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.RespectDensity), 3);
        AverageRespectConcentrationMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.RespectConcentration), 3);
        AverageThanksCoinRateMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ThanksCoinRate), 3);
        AverageThanksHelpRateMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ThanksHelpRate), 3);
        AverageThanksIdeaRateMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ThanksIdeaRate), 3);
        AverageThanksChallengeRateMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ThanksChallengeRate), 3);
        AverageThanksBridgeRateMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ThanksBridgeRate), 3);
        AverageThanksConcentrationMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ThanksConcentration), 3);
        AverageThanksDiversityIndexMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ThanksDiversityIndex), 3);
        AverageThanksToEmergenceContributionMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.ThanksToEmergenceContribution), 3);
        AverageChallengeAcceptanceScoreMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageChallengeAcceptanceScore), 3);
        AverageRespectReconfigurationBoostMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageRespectReconfigurationBoost), 3);
        AverageRespectEmergenceComponentMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageRespectEmergenceComponent), 3);
        AverageThanksCoinToReconfigurationContributionMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageThanksCoinToReconfigurationContribution), 3);
        AverageThanksCoinToSerendipityContributionMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageThanksCoinToSerendipityContribution), 3);
        AverageThanksCoinToEmergenceContributionMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageThanksCoinToEmergenceContribution), 3);
        PopularityTrapRunCount = RunFingerprints.Count(item => item.PopularityTrapRate >= 0.40);
        EmergentChallengeThanksRateMean = RunFingerprints.Count == 0
            ? 0
            : Math.Round(RunFingerprints.Where(item => string.Equals(item.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase)).Select(item => item.ThanksChallengeRate).DefaultIfEmpty(0).Average(), 3);
        LearningChallengeThanksRateMean = RunFingerprints.Count == 0
            ? 0
            : Math.Round(RunFingerprints.Where(item => string.Equals(item.FinalPhase, SimulationPhase.Learning, StringComparison.OrdinalIgnoreCase)).Select(item => item.ThanksChallengeRate).DefaultIfEmpty(0).Average(), 3);
        TrustCapacityExceededRunCount = RunFingerprints.Count(item => item.TrustCapacityExceededRate > 0);
        OvertrustedCompleteNetworkRunCount = RunFingerprints.Count(item => string.Equals(item.TrustNetworkType, "過信完全ネットワーク型", StringComparison.OrdinalIgnoreCase));
        SelectiveTrustNetworkRunCount = RunFingerprints.Count(item => string.Equals(item.TrustNetworkType, "選択的信頼ネットワーク型", StringComparison.OrdinalIgnoreCase));
        FragileTrustNetworkRunCount = RunFingerprints.Count(item => string.Equals(item.TrustNetworkType, "脆弱信頼ネットワーク型", StringComparison.OrdinalIgnoreCase));
        AverageKnowledgeStockMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageKnowledgeStock), 3);
        AverageKnowledgeDiversityMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageKnowledgeDiversity), 3);
        AverageKnowledgeRewiringScoreMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageKnowledgeRewiringScore), 3);
        AverageExplorationScoreMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageExplorationScore), 3);
        AverageSerendipityScoreMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageSerendipityScore), 3);
        AverageKnowledgeRecombinationScoreMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageKnowledgeRecombinationScore), 3);
        AverageKnowledgeReconfigurationScoreMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageKnowledgeReconfigurationScore), 3);
        SerendipityOccurredRunCount = RunFingerprints.Count(item => item.SerendipityOccurredCount > 0);
        SerendipityOccurredRate = RunFingerprints.Count == 0 ? 0 : Math.Round(SerendipityOccurredRunCount / (double)RunFingerprints.Count, 3);
        SerendipityToEmergenceLinkRunCount = RunFingerprints.Count(item => item.SerendipityToEmergenceLinkCount > 0);
        SerendipityToEmergenceRate = RunFingerprints.Count == 0 ? 0 : Math.Round(SerendipityToEmergenceLinkRunCount / (double)RunFingerprints.Count, 3);
        ChallengeResolvedRunCount = RunFingerprints.Count(item => item.ChallengeResolved);
        AverageChallengeResolutionScoreMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AverageChallengeResolutionScore), 3);
        AverageAdaptationDurationMean = RunFingerprints.Count == 0 ? 0 : Math.Round(RunFingerprints.Average(item => item.AdaptationDuration), 3);
        ShockOccurredRunCount = RunFingerprints.Count(item => item.ShockOccurred);
        EmergentReachedRunCount = RunFingerprints.Count(item => string.Equals(item.FinalPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase));
        ThresholdSweepSummary = await GetThresholdSweepSummaryAsync(id);

        return Page();
    }

    public string GetActionTimelineSummaryJson()
    {
        return JsonSerializer.Serialize(ActionTimelineSummary, JsonOptions);
    }

    public async Task<IActionResult> OnPostStopAsync(int id)
    {
        var experiment = await db.Experiments.FirstOrDefaultAsync(item => item.Id == id);
        if (experiment is null)
        {
            return RedirectToPage("/Experiments/Index");
        }

        if (experiment.Status == ExperimentStatus.Running)
        {
            experiment.Status = ExperimentStatus.StopRequested;
            await db.SaveChangesAsync();
            ExperimentMessage = "停止要求を受け付けました。現在実行中のRunが終わった後に停止します。";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostMarkFailedAsync(int id)
    {
        var updated = await cleanupService.MarkExperimentAsFailedAsync(id);
        ExperimentMessage = updated
            ? "Experimentを失敗扱いにしました。"
            : "Experimentが見つかりませんでした。";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostForceDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var result = await cleanupService.ForceDeleteExperimentAsync(
            id,
            async (sweepId, innerCancellationToken) =>
            {
                await parameterSweepRunner.RecalculateSweepStatusAsync(sweepId, innerCancellationToken);
            },
            cancellationToken);

        TempData["CleanupMessage"] = result.DeletedExperiments > 0
            ? result.ToJapaneseMessage()
            : "削除対象のExperimentがありませんでした。";
        return RedirectToPage("/Experiments/Index");
    }

    public async Task<IActionResult> OnPostDeleteRunAsync(int id, int runId)
    {
        var run = await db.ExperimentRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == runId && item.ExperimentId == id);

        if (run is null)
        {
            return RedirectToPage(new { id });
        }

        await SimulationDataDeletion.DeleteSimulationProjectAsync(db, run.SimulationProjectId);
        ExperimentMessage = "Run と関連シミュレーションを削除しました。";
        return RedirectToPage(new { id });
    }

    private static List<PhaseDistributionRow> GetPhaseDistribution(IReadOnlyCollection<ExperimentRun> runs)
    {
        var phases = new[]
        {
            SimulationPhase.Forming,
            SimulationPhase.Learning,
            SimulationPhase.Adaptation,
            SimulationPhase.Emergent,
            SimulationPhase.Stable,
            SimulationPhase.Silo,
            SimulationPhase.Chaos,
            SimulationPhase.Collapse
        };

        return phases
            .Select(phase => new PhaseDistributionRow
            {
                Phase = phase,
                Count = runs.Count(run => run.FinalPhase == phase)
            })
            .ToList();
    }

    private async Task<List<PhaseTransitionRow>> GetPhaseTransitionsAsync(int experimentId)
    {
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync();

        var steps = await db.SimulationSteps
            .Where(step => simulationProjectIds.Contains(step.SimulationProjectId))
            .OrderBy(step => step.SimulationProjectId)
            .ThenBy(step => step.StepNo)
            .ToListAsync();

        var transitions = steps
            .GroupBy(step => step.SimulationProjectId)
            .SelectMany(group => group.Zip(group.Skip(1), (from, to) => new { from.Phase, ToPhase = to.Phase }))
            .GroupBy(item => new { item.Phase, item.ToPhase })
            .Select(group => new PhaseTransitionRow
            {
                FromPhase = group.Key.Phase,
                ToPhase = group.Key.ToPhase,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.FromPhase)
            .ThenBy(item => item.ToPhase)
            .ToList();

        return transitions;
    }

    private async Task<List<TrustTrendPoint>> GetTrustTrendAsync(int experimentId)
    {
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync();

        var actions = await db.AgentActions
            .Where(action => simulationProjectIds.Contains(action.SimulationProjectId) && action.TrustDelta.HasValue)
            .OrderBy(action => action.StepNo)
            .ToListAsync();

        return actions
            .GroupBy(action => action.StepNo)
            .Select(group => new TrustTrendPoint
            {
                StepNo = group.Key,
                AverageTrustDelta = Math.Round(group.Average(action => action.TrustDelta ?? 0), 2),
                Count = group.Count()
            })
            .OrderBy(point => point.StepNo)
            .ToList();
    }

    private async Task<List<TrustStatePoint>> GetTrustStateTrendAsync(int experimentId)
    {
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync();

        var snapshots = await db.TrustSnapshots
            .Where(snapshot => simulationProjectIds.Contains(snapshot.SimulationProjectId))
            .OrderBy(snapshot => snapshot.StepNo)
            .ToListAsync();

        return snapshots
            .GroupBy(snapshot => snapshot.StepNo)
            .Select(group => new TrustStatePoint
            {
                StepNo = group.Key,
                AverageTrust = Math.Round(group.Average(snapshot => snapshot.TrustValue), 2),
                AverageAbsTrust = Math.Round(group.Average(snapshot => Math.Abs(snapshot.TrustValue)), 2),
                Count = group.Count()
            })
            .OrderBy(point => point.StepNo)
            .ToList();
    }

    private async Task<List<ExperimentActionTimelinePoint>> GetActionTimelineSummaryAsync(int experimentId)
    {
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync();

        var actions = await db.AgentActions
            .Where(action => simulationProjectIds.Contains(action.SimulationProjectId))
            .OrderBy(action => action.SimulationProjectId)
            .ThenBy(action => action.StepNo)
            .ToListAsync();

        return actions
            .GroupBy(action => new { action.SimulationProjectId, action.StepNo })
            .Select(group =>
            {
                var distribution = ActionDistributionCalculator.Calculate(group.Select(action => action.Action));
                return new
                {
                    group.Key.StepNo,
                    ShareInfoRate = distribution.ShareInfoRate,
                    AskHelpRate = distribution.AskHelpRate,
                    ProposeIdeaRate = distribution.ProposeIdeaRate,
                    CriticizeRate = distribution.CriticizeRate,
                    WorkAloneRate = distribution.WorkAloneRate,
                    SupportOtherRate = distribution.SupportOtherRate,
                    WaitRate = distribution.WaitRate,
                    OtherRate = distribution.OtherRate
                };
            })
            .GroupBy(point => point.StepNo)
            .Select(group => new ExperimentActionTimelinePoint
            {
                StepNo = group.Key,
                RunCount = group.Count(),
                ShareInfoRate = Math.Round(group.Average(point => point.ShareInfoRate), 4),
                AskHelpRate = Math.Round(group.Average(point => point.AskHelpRate), 4),
                ProposeIdeaRate = Math.Round(group.Average(point => point.ProposeIdeaRate), 4),
                CriticizeRate = Math.Round(group.Average(point => point.CriticizeRate), 4),
                WorkAloneRate = Math.Round(group.Average(point => point.WorkAloneRate), 4),
                SupportOtherRate = Math.Round(group.Average(point => point.SupportOtherRate), 4),
                WaitRate = Math.Round(group.Average(point => point.WaitRate), 4),
                OtherRate = Math.Round(group.Average(point => point.OtherRate), 4)
            })
            .OrderBy(point => point.StepNo)
            .ToList();
    }

    private async Task<List<PhaseActionDistributionPoint>> GetPhaseActionDistributionAsync(int experimentId)
    {
        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync();

        var stepPhases = await db.SimulationSteps
            .Where(step => simulationProjectIds.Contains(step.SimulationProjectId))
            .Select(step => new
            {
                step.SimulationProjectId,
                step.StepNo,
                step.Phase
            })
            .ToListAsync();

        var phaseByStep = stepPhases.ToDictionary(
            step => (step.SimulationProjectId, step.StepNo),
            step => step.Phase);

        var actions = await db.AgentActions
            .Where(action => simulationProjectIds.Contains(action.SimulationProjectId))
            .Select(action => new
            {
                action.SimulationProjectId,
                action.StepNo,
                action.Action
            })
            .ToListAsync();

        return actions
            .GroupBy(action => phaseByStep.GetValueOrDefault(
                (action.SimulationProjectId, action.StepNo),
                SimulationPhase.Forming))
            .Select(group =>
            {
                var distribution = ActionDistributionCalculator.Calculate(group.Select(action => action.Action));
                return new PhaseActionDistributionPoint
                {
                    Phase = group.Key,
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
                    OtherRate = distribution.OtherRate
                };
            })
            .OrderBy(point => GetPhaseOrder(point.Phase))
            .ThenBy(point => point.Phase)
            .ToList();
    }

    private async Task<List<ExperimentPrecursorPoint>> GetPrecursorSummaryAsync(int experimentId)
    {
        var analyses = await GetProjectAnalysesAsync(experimentId);
        if (analyses.Count == 0)
        {
            return [];
        }

        return analyses
            .SelectMany(analysis => analysis.PrecursorPoints)
            .GroupBy(point => point.StepNo)
            .Select(group =>
            {
                var avgSilo = Math.Round(group.Average(point => point.SiloRiskScore), 4);
                var avgStable = Math.Round(group.Average(point => point.StableScore), 4);
                var avgEmergent = Math.Round(group.Average(point => point.EmergentScore), 4);

                var signal = avgSilo >= avgStable && avgSilo >= avgEmergent
                    ? "\u30B5\u30A4\u30ED\u4E88\u5146"
                    : avgEmergent >= avgStable
                        ? "\u5275\u767A\u4E88\u5146"
                        : "\u5B89\u5B9A\u4E88\u5146";

                return new ExperimentPrecursorPoint
                {
                    StepNo = group.Key,
                    RunCount = group.Count(),
                    AverageSiloRiskScore = avgSilo,
                    AverageStableScore = avgStable,
                    AverageEmergentScore = avgEmergent,
                    MainSignal = signal
                };
            })
            .OrderBy(point => point.StepNo)
            .ToList();
    }

    private async Task<List<PhaseTransitionInsight>> GetPhaseTransitionInsightsAsync(int experimentId)
    {
        var analyses = await GetProjectAnalysesAsync(experimentId);
        return analyses
            .SelectMany(analysis => analysis.PhaseTransitionInsights)
            .ToList();
    }

    private async Task<List<EmergenceFingerprint>> GetRunFingerprintsAsync(int experimentId)
    {
        var analyses = await GetProjectAnalysesAsync(experimentId);
        return analyses
            .Select(analysis => analysis.Fingerprint)
            .OrderBy(item => item.RunNo ?? int.MaxValue)
            .ThenBy(item => item.SimulationProjectId)
            .ToList();
    }

    private async Task<List<ProjectAnalysisBundle>> GetProjectAnalysesAsync(int experimentId)
    {
        var projects = await db.SimulationProjects
            .Include(project => project.Agents)
            .Include(project => project.Metrics)
            .Where(project => project.ExperimentId == experimentId)
            .OrderBy(project => project.Id)
            .ToListAsync();

        if (projects.Count == 0)
        {
            return [];
        }

        var runsByProjectId = await db.ExperimentRuns
            .Where(run => run.ExperimentId == experimentId)
            .ToDictionaryAsync(run => run.SimulationProjectId, run => run);

        var projectIds = projects.Select(project => project.Id).ToList();
        var steps = await db.SimulationSteps
            .Where(step => projectIds.Contains(step.SimulationProjectId))
            .OrderBy(step => step.SimulationProjectId)
            .ThenBy(step => step.StepNo)
            .ToListAsync();
        var actions = await db.AgentActions
            .Include(action => action.Agent)
            .Where(action => projectIds.Contains(action.SimulationProjectId))
            .OrderBy(action => action.SimulationProjectId)
            .ThenBy(action => action.StepNo)
            .ThenBy(action => action.Id)
            .ToListAsync();
        var trustSnapshots = await db.TrustSnapshots
            .Where(snapshot => projectIds.Contains(snapshot.SimulationProjectId))
            .OrderBy(snapshot => snapshot.SimulationProjectId)
            .ThenBy(snapshot => snapshot.StepNo)
            .ThenBy(snapshot => snapshot.SourceAgentId)
            .ThenBy(snapshot => snapshot.TargetAgentId)
            .ToListAsync();

        var actionsByProjectId = actions.GroupBy(action => action.SimulationProjectId).ToDictionary(group => group.Key, group => group.ToList());
        var stepsByProjectId = steps.GroupBy(step => step.SimulationProjectId).ToDictionary(group => group.Key, group => group.ToList());
        var snapshotsByProjectId = trustSnapshots.GroupBy(snapshot => snapshot.SimulationProjectId).ToDictionary(group => group.Key, group => group.ToList());

        List<ProjectAnalysisBundle> bundles = [];
        foreach (var project in projects)
        {
            var projectActions = actionsByProjectId.GetValueOrDefault(project.Id, new List<AgentAction>());
            var projectSteps = stepsByProjectId.GetValueOrDefault(project.Id, new List<SimulationStep>());
            var projectSnapshots = snapshotsByProjectId.GetValueOrDefault(project.Id, new List<TrustSnapshot>());
            var actionRecords = projectActions
                .Select(action => new PhaseTransitionActionRecord
                {
                    StepNo = action.StepNo,
                    AgentName = action.Agent?.Name ?? project.Agents.FirstOrDefault(agent => agent.Id == action.AgentId)?.Name ?? "",
                    Action = action.Action,
                    TargetAgentName = string.IsNullOrWhiteSpace(action.TargetAgentName) ? "-" : action.TargetAgentName,
                    TrustBefore = action.TrustBefore,
                    TrustDelta = action.TrustDelta,
                    TrustAfter = action.TrustAfter
                })
                .ToList();
            var finalRows = GetFinalTrustRows(project, projectSnapshots);
            var finalMetrics = CalculateNetworkMetrics(project.Agents, finalRows, project.EffectiveTrustThreshold);
            var knowledgeTimeline = KnowledgeAnalysisService.BuildTimeline(projectSteps);
            var thresholdSweep = NetworkMetricsCalculator.StandardThresholdSweepValues
                .Select(threshold =>
                {
                    var metrics = CalculateNetworkMetrics(project.Agents, finalRows, threshold);
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
            var edgeRowsByStep = NetworkMetricsCalculator.BuildEdgeRowsByStep(project.Agents, projectSnapshots, project.CurrentStep);
            var stepStates = BuildPhaseTransitionStates(project, projectActions, projectSteps, projectSnapshots, edgeRowsByStep);
            var insights = PhaseTransitionInspector.BuildInsights(
                stepStates,
                actionRecords,
                edgeRowsByStep,
                windowSize: 3,
                effectiveTrustThreshold: project.EffectiveTrustThreshold);
            var precursorPoints = PrecursorAnalysisService.BuildPoints(stepStates, project.PsychologicalSafetyLevel);
            runsByProjectId.TryGetValue(project.Id, out var run);
            var fingerprint = EmergenceFingerprintService.Build(
                project,
                project.Name,
                project.Id,
                projectActions,
                knowledgeTimeline,
                insights,
                thresholdSweep,
                finalMetrics,
                run?.RunNo);

            bundles.Add(new ProjectAnalysisBundle
            {
                ProjectId = project.Id,
                PhaseTransitionInsights = insights,
                PrecursorPoints = precursorPoints,
                Fingerprint = fingerprint
            });
        }

        return bundles;
    }

    private async Task<List<ExperimentThresholdSweepPoint>> GetThresholdSweepSummaryAsync(int experimentId)
    {
        var projects = await db.SimulationProjects
            .Include(project => project.Agents)
            .Where(project => project.ExperimentId == experimentId)
            .OrderBy(project => project.Id)
            .ToListAsync();

        if (projects.Count == 0)
        {
            return [];
        }

        var projectIds = projects.Select(project => project.Id).ToList();
        var snapshots = await db.TrustSnapshots
            .Where(snapshot => projectIds.Contains(snapshot.SimulationProjectId))
            .OrderBy(snapshot => snapshot.SimulationProjectId)
            .ThenBy(snapshot => snapshot.StepNo)
            .ThenBy(snapshot => snapshot.SourceAgentId)
            .ThenBy(snapshot => snapshot.TargetAgentId)
            .ToListAsync();

        var finalRowsByProjectId = projects.ToDictionary(
            project => project.Id,
            project => GetFinalTrustRows(project, snapshots));

        return NetworkMetricsCalculator.StandardThresholdSweepValues
            .Select(threshold =>
            {
                var metricsList = projects
                    .Select(project => CalculateNetworkMetrics(project.Agents, finalRowsByProjectId[project.Id], threshold))
                    .ToList();

                return new ExperimentThresholdSweepPoint
                {
                    Threshold = threshold,
                    AverageEffectiveNetworkDensity = Math.Round(metricsList.Average(item => item.EffectiveNetworkDensity), 3),
                    AverageStrongLinkCount = Math.Round(metricsList.Average(item => item.StrongLinkCount), 3),
                    AverageWeakLinkCount = Math.Round(metricsList.Average(item => item.WeakLinkCount), 3),
                    AverageComponentCount = Math.Round(metricsList.Average(item => item.ComponentCount), 3),
                    AverageIsolatedCount = Math.Round(metricsList.Average(item => item.IsolatedCount), 3),
                    RunCount = metricsList.Count
                };
            })
            .ToList();
    }

    private static List<NetworkTrustEdgeRef> GetFinalTrustRows(
        SimulationProject project,
        IReadOnlyCollection<TrustSnapshot> allSnapshots)
    {
        var projectSnapshots = allSnapshots
            .Where(snapshot => snapshot.SimulationProjectId == project.Id)
            .ToList();

        if (projectSnapshots.Count == 0)
        {
            return NetworkMetricsCalculator.CreateRowsFromAgents(project.Agents);
        }

        var finalStepNo = projectSnapshots
            .Where(snapshot => snapshot.StepNo <= project.CurrentStep)
            .Select(snapshot => (int?)snapshot.StepNo)
            .Max();

        if (!finalStepNo.HasValue)
        {
            return NetworkMetricsCalculator.CreateRowsFromAgents(project.Agents);
        }

        return projectSnapshots
            .Where(snapshot => snapshot.StepNo == finalStepNo.Value)
            .Select(snapshot => new NetworkTrustEdgeRef(
                snapshot.SourceAgentId,
                snapshot.TargetAgentId,
                snapshot.SourceAgentName,
                snapshot.TargetAgentName,
                TrustJsonUtility.Clamp(snapshot.TrustValue)))
            .ToList();
    }

    private static NetworkMetricsResult CalculateNetworkMetrics(
        IReadOnlyCollection<Agent> agents,
        IReadOnlyCollection<NetworkTrustEdgeRef> rows,
        double threshold)
    {
        return NetworkMetricsCalculator.Calculate(
            agents.OrderBy(agent => agent.Id)
                .Select(agent => new NetworkAgentRef(agent.Id, agent.Name))
                .ToList(),
            rows,
            threshold);
    }

    private static List<PhaseTransitionStepState> BuildPhaseTransitionStates(
        SimulationProject project,
        IReadOnlyCollection<AgentAction> actions,
        IReadOnlyCollection<SimulationStep> steps,
        IReadOnlyCollection<TrustSnapshot> trustSnapshots,
        IReadOnlyDictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>> edgeRowsByStep)
    {
        var actionByStep = ActionDistributionCalculator.BuildByStep(
            actions,
            action => action.StepNo,
            action => action.Action);
        var metricsByStep = NetworkMetricsCalculator.BuildMetricsByStep(
            project.Agents,
            trustSnapshots,
            project.EffectiveTrustThreshold,
            project.CurrentStep);
        var phaseByStep = steps.ToDictionary(step => step.StepNo, step => step.Phase);
        var knowledgeByStep = KnowledgeAnalysisService.BuildTimeline(steps).ToDictionary(point => point.StepNo);
        var maxEdges = Math.Max(project.Agents.Count * Math.Max(project.Agents.Count - 1, 0), 1);

        List<PhaseTransitionStepState> states = [];
        for (var stepNo = 1; stepNo <= project.CurrentStep; stepNo++)
        {
            if (!metricsByStep.TryGetValue(stepNo, out var metrics))
            {
                continue;
            }

            actionByStep.TryGetValue(stepNo, out var action);
            knowledgeByStep.TryGetValue(stepNo, out var knowledge);
            states.Add(new PhaseTransitionStepState
            {
                StepNo = stepNo,
                Phase = phaseByStep.GetValueOrDefault(stepNo, stepNo <= 2 ? SimulationPhase.Forming : project.Phase),
                ShareInfoRate = action?.ShareInfoRate ?? 0,
                AskHelpRate = action?.AskHelpRate ?? 0,
                ProposeIdeaRate = action?.ProposeIdeaRate ?? 0,
                CriticizeRate = action?.CriticizeRate ?? 0,
                WorkAloneRate = action?.WorkAloneRate ?? 0,
                SupportOtherRate = action?.SupportOtherRate ?? 0,
                WaitRate = action?.WaitRate ?? 0,
                OtherRate = action?.OtherRate ?? 0,
                AverageTrust = metrics.AverageTrust,
                EffectiveNetworkDensity = metrics.EffectiveNetworkDensity,
                NewStrongLinkRate = CalculateNewStrongLinkRate(
                    edgeRowsByStep,
                    stepNo,
                    project.EffectiveTrustThreshold,
                    maxEdges),
                StrongLinkCount = metrics.StrongLinkCount,
                WeakLinkCount = metrics.WeakLinkCount,
                ComponentCount = metrics.ComponentCount,
                IsolatedCount = metrics.IsolatedCount,
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

    private static int GetPhaseOrder(string? phase) => phase switch
    {
        SimulationPhase.Collapse => 0,
        SimulationPhase.Chaos => 1,
        SimulationPhase.Silo => 2,
        SimulationPhase.Adaptation => 3,
        SimulationPhase.Emergent => 4,
        SimulationPhase.Stable => 5,
        SimulationPhase.Learning => 6,
        SimulationPhase.Forming => 7,
        _ => 99
    };

    private sealed class ProjectAnalysisBundle
    {
        public int ProjectId { get; init; }
        public List<PhaseTransitionInsight> PhaseTransitionInsights { get; init; } = [];
        public List<PrecursorPoint> PrecursorPoints { get; init; } = [];
        public EmergenceFingerprint Fingerprint { get; init; } = new();
    }
}
