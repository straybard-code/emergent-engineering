using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Services;

public sealed class SimulationRunner(
    AppDbContext db,
    MockLlmService mockLlmService,
    OpenAiLlmService openAiLlmService) : ISimulationRunner
{
    private const int PhaseWindowSize = 10;
    private const int SummaryStepInterval = 5;
    private const int SummaryTrustSnapshotInterval = 10;
    private const int RetentionWindowStepCount = 10;
    private const string FullPersistenceMode = "Full";
    private const string SummaryPersistenceMode = "Summary";
    private const string MinimalPersistenceMode = "Minimal";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task<SimulationStep?> RunOneStepAsync(int simulationId, CancellationToken cancellationToken = default)
    {
        var project = await db.SimulationProjects
            .Include(item => item.Agents.OrderBy(agent => agent.Id))
            .FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);

        if (project is null)
        {
            return null;
        }

        if (project.CurrentStep >= project.TotalSteps)
        {
            if (!string.Equals(project.Status, SimulationStatus.Failed, StringComparison.OrdinalIgnoreCase))
            {
                project.CurrentStep = Math.Max(project.CurrentStep, project.TotalSteps);
                project.Status = SimulationStatus.Completed;
                await db.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        var stepNo = project.CurrentStep + 1;
        project.Status = SimulationStatus.Running;
        var shouldPersistActions = ShouldPersistActions(project, stepNo);

        var recentActions = await db.AgentActions
            .Include(action => action.Agent)
            .Where(action => action.SimulationProjectId == simulationId && action.StepNo == project.CurrentStep)
            .OrderBy(action => action.Id)
            .ToListAsync(cancellationToken);
        var previousSteps = await db.SimulationSteps
            .Where(step => step.SimulationProjectId == simulationId)
            .OrderBy(step => step.StepNo)
            .ToListAsync(cancellationToken);
        var previousKnowledgeTimeline = KnowledgeAnalysisService.BuildTimeline(previousSteps);
        var previousKnowledgePoint = previousKnowledgeTimeline.LastOrDefault();
        List<AgentAction> currentStepActions = [];
        var trustDynamicsState = new TrustDynamicsStepState();
        var touchedTrustEdges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var agent in project.Agents)
        {
            var prompt = BuildPrompt(project, agent, recentActions);
            var llm = await CompleteAgentTurnAsync(project, prompt, cancellationToken);
            if (!AgentActionType.All.Contains(llm.Action))
            {
                llm.Action = AgentActionType.Wait;
            }

            agent.Memory = string.IsNullOrWhiteSpace(llm.Memory) ? agent.Memory : llm.Memory.Trim();

            var agentAction = new AgentAction
            {
                SimulationProjectId = project.Id,
                StepNo = stepNo,
                AgentId = agent.Id,
                Message = string.IsNullOrWhiteSpace(llm.Message) ? $"{agent.Name} waits for the next clear signal." : llm.Message.Trim(),
                Memory = agent.Memory,
                Action = llm.Action,
                TargetAgentName = llm.TargetAgentName?.Trim() ?? "",
                RawLlmResponse = string.IsNullOrWhiteSpace(llm.RawResponse) ? JsonSerializer.Serialize(llm, JsonOptions) : llm.RawResponse,
                CreatedAt = DateTime.UtcNow
            };

            var trustMetrics = ApplyTrustUpdate(project, agent, agentAction, trustDynamicsState, touchedTrustEdges);
            agentAction.TrustBefore = trustMetrics.Before;
            agentAction.TrustDelta = trustMetrics.Delta;
            agentAction.TrustAfter = trustMetrics.After;
            currentStepActions.Add(agentAction);
            if (shouldPersistActions)
            {
                db.AgentActions.Add(agentAction);
            }
        }

        var stepActionSummary = ActionDistributionCalculator.Calculate(currentStepActions.Select(action => action.Action));
        var knowledgePoint = ApplyKnowledgeShockAndChallenge(project, stepNo, stepActionSummary, previousKnowledgePoint);
        var trustDynamicsSummary = ApplyTrustDynamics(project, touchedTrustEdges, trustDynamicsState);
        var thanksCoinSummary = ThanksCoinService.Apply(project, stepActionSummary, previousKnowledgePoint, stepNo);

        knowledgePoint.KnowledgeRecombinationScore = Clamp01(
            knowledgePoint.KnowledgeRecombinationScore
            + (thanksCoinSummary.ReconfigurationDelta * 0.50)
            + (thanksCoinSummary.BridgeDelta * 0.25));
        knowledgePoint.KnowledgeReconfigurationScore = Clamp01(
            knowledgePoint.KnowledgeReconfigurationScore
            + thanksCoinSummary.ReconfigurationDelta
            + (thanksCoinSummary.BridgeDelta * 0.25));
        knowledgePoint.SerendipityScore = Clamp01(
            knowledgePoint.SerendipityScore
            + thanksCoinSummary.SerendipityDelta
            + (thanksCoinSummary.BridgeDelta * 0.50));
        knowledgePoint.KnowledgeRewiringScore = Clamp01(
            knowledgePoint.KnowledgeRewiringScore
            + (thanksCoinSummary.BridgeDelta * 0.35)
            + (thanksCoinSummary.RewiringDelta * 0.25));
        knowledgePoint.CrossDomainExposure = Clamp01(
            knowledgePoint.CrossDomainExposure
            + thanksCoinSummary.CrossDomainExposureDelta);
        knowledgePoint.PsychologicalSafetyActionEffect = Math.Round(
            knowledgePoint.PsychologicalSafetyActionEffect + thanksCoinSummary.PsychologicalSafetyDelta,
            4);
        knowledgePoint.PsychologicalSafetyRecombinationBonus = Math.Round(
            knowledgePoint.PsychologicalSafetyRecombinationBonus + (thanksCoinSummary.PsychologicalSafetyDelta * 0.5),
            4);
        knowledgePoint.PsychologicalSafetySerendipityBonus = Math.Round(
            knowledgePoint.PsychologicalSafetySerendipityBonus + (thanksCoinSummary.BridgeDelta * 0.5),
            4);
        knowledgePoint.PsychologicalSafetyEmergenceBonus = Math.Round(
            knowledgePoint.PsychologicalSafetyEmergenceBonus + (thanksCoinSummary.PsychologicalSafetyDelta * 0.25),
            4);
        knowledgePoint.AverageRespect = thanksCoinSummary.AverageRespect;
        knowledgePoint.RespectDensity = thanksCoinSummary.RespectDensity;
        knowledgePoint.RespectStrongLinks = thanksCoinSummary.RespectStrongLinks;
        knowledgePoint.RespectWeakLinks = thanksCoinSummary.RespectWeakLinks;
        knowledgePoint.RespectConcentration = thanksCoinSummary.RespectConcentration;
        knowledgePoint.RespectJson = thanksCoinSummary.RespectJson;
        knowledgePoint.ThanksCoinOccurred = thanksCoinSummary.Occurred;
        knowledgePoint.ThanksCoinCount = thanksCoinSummary.Count;
        knowledgePoint.ThanksCoinHelpCount = thanksCoinSummary.HelpCount;
        knowledgePoint.ThanksCoinIdeaCount = thanksCoinSummary.IdeaCount;
        knowledgePoint.ThanksCoinChallengeCount = thanksCoinSummary.ChallengeCount;
        knowledgePoint.ThanksCoinBridgeCount = thanksCoinSummary.BridgeCount;
        knowledgePoint.ThanksCoinRespectDelta = thanksCoinSummary.RespectDelta;
        knowledgePoint.ThanksCoinTrustDelta = thanksCoinSummary.TrustDelta;
        knowledgePoint.ThanksCoinReconfigurationDelta = thanksCoinSummary.ReconfigurationDelta;
        knowledgePoint.ThanksCoinSerendipityDelta = thanksCoinSummary.SerendipityDelta;
        knowledgePoint.ThanksCoinBridgeDelta = thanksCoinSummary.BridgeDelta;
        knowledgePoint.ThanksConcentration = thanksCoinSummary.ThanksConcentration;

        var intellectualRespectSummary = IntellectualRespectService.Apply(project, stepActionSummary, previousKnowledgePoint, stepNo);
        var constructiveCriticismRate = KnowledgeAnalysisService.CalculateConstructiveCriticismRate(
            stepActionSummary.CriticizeRate,
            project.PsychologicalSafetyLevel);
        var destructiveCriticismRate = KnowledgeAnalysisService.CalculateDestructiveCriticismRate(
            stepActionSummary.CriticizeRate,
            project.PsychologicalSafetyLevel);
        var thanksChallengeRate = thanksCoinSummary.Count == 0 ? 0 : thanksCoinSummary.ChallengeCount / (double)thanksCoinSummary.Count;
        var thanksBridgeRate = thanksCoinSummary.Count == 0 ? 0 : thanksCoinSummary.BridgeCount / (double)thanksCoinSummary.Count;
        var challengeAcceptanceScore = KnowledgeAnalysisService.CalculateChallengeAcceptanceScore(
            project.PsychologicalSafetyLevel,
            thanksCoinSummary.AverageRespect,
            intellectualRespectSummary.AverageIntellectualRespect,
            project.IntellectualRespectChallengeSensitivity,
            thanksChallengeRate,
            project.ConstructiveCriticismBonus);
        var respectReconfigurationBoost = KnowledgeAnalysisService.CalculateRespectReconfigurationBoost(
            thanksCoinSummary.AverageRespect,
            thanksCoinSummary.RespectDensity,
            thanksChallengeRate,
            thanksBridgeRate);
        var thanksChallengeEffect = KnowledgeAnalysisService.CalculateThanksChallengeEffect(thanksCoinSummary.ChallengeCount, project.Agents.Count);
        var thanksBridgeEffect = KnowledgeAnalysisService.CalculateThanksBridgeEffect(thanksCoinSummary.BridgeCount, project.Agents.Count);
        var diversityRespectEffect = KnowledgeAnalysisService.CalculateDiversityRespectEffect(
            project.ThanksCoinDiversityBonus,
            thanksChallengeRate,
            thanksBridgeRate);
        var respectEmergenceComponent = KnowledgeAnalysisService.CalculateRespectEmergenceComponent(
            thanksCoinSummary.AverageRespect,
            thanksCoinSummary.RespectDensity,
            challengeAcceptanceScore,
            thanksChallengeRate,
            thanksBridgeRate,
            diversityRespectEffect);
        var popularityTrapPenalty = KnowledgeAnalysisService.CalculatePopularityTrapPenalty(
            thanksCoinSummary.ThanksConcentration,
            thanksCoinSummary.AverageRespect);
        var popularityTrapDetected = thanksCoinSummary.ThanksConcentration >= 0.60;
        var thanksCoinToReconfigurationContribution = Math.Round(
            respectReconfigurationBoost
            + (thanksChallengeEffect * 0.20)
            + (thanksBridgeEffect * 0.15)
            + diversityRespectEffect,
            4);
        var thanksCoinToSerendipityContribution = Math.Round(
            (thanksBridgeEffect * 0.20)
            + diversityRespectEffect
            + (challengeAcceptanceScore * 0.05),
            4);
        var thanksCoinToEmergenceContribution = Math.Round(
            (respectEmergenceComponent * 0.50)
            + (challengeAcceptanceScore * 0.20)
            + (thanksChallengeEffect * 0.15)
            + (thanksBridgeEffect * 0.15)
            - popularityTrapPenalty,
            4);

        knowledgePoint.ConstructiveCriticismRate = constructiveCriticismRate;
        knowledgePoint.DestructiveCriticismRate = destructiveCriticismRate;
        knowledgePoint.ChallengeAcceptanceScore = challengeAcceptanceScore;
        knowledgePoint.RespectReconfigurationBoost = respectReconfigurationBoost;
        knowledgePoint.ThanksChallengeEffect = thanksChallengeEffect;
        knowledgePoint.ThanksBridgeEffect = thanksBridgeEffect;
        knowledgePoint.DiversityRespectEffect = diversityRespectEffect;
        knowledgePoint.RespectEmergenceComponent = respectEmergenceComponent;
        knowledgePoint.PopularityTrapPenalty = popularityTrapPenalty;
        knowledgePoint.PopularityTrapDetected = popularityTrapDetected;
        knowledgePoint.ThanksCoinToReconfigurationContribution = thanksCoinToReconfigurationContribution;
        knowledgePoint.ThanksCoinToSerendipityContribution = thanksCoinToSerendipityContribution;
        knowledgePoint.ThanksCoinToEmergenceContribution = thanksCoinToEmergenceContribution;

        knowledgePoint.KnowledgeRecombinationScore = Clamp01(
            knowledgePoint.KnowledgeRecombinationScore
            + (challengeAcceptanceScore * 0.05)
            + (diversityRespectEffect * 0.05));
        knowledgePoint.KnowledgeReconfigurationScore = Clamp01(
            knowledgePoint.KnowledgeReconfigurationScore
            + respectReconfigurationBoost
            + (thanksChallengeEffect * 0.20)
            + (thanksBridgeEffect * 0.15)
            + diversityRespectEffect);
        knowledgePoint.SerendipityScore = Clamp01(
            knowledgePoint.SerendipityScore
            + (thanksBridgeEffect * 0.20)
            + diversityRespectEffect
            + (challengeAcceptanceScore * 0.05)
            - popularityTrapPenalty * 0.20);
        knowledgePoint.KnowledgeRewiringScore = Clamp01(
            knowledgePoint.KnowledgeRewiringScore
            + (diversityRespectEffect * 0.10));

        var intellectualRespectDerived = project.EnableIntellectualRespect
            ? IntellectualRespectService.BuildDerivedScores(
                intellectualRespectSummary,
                thanksCoinSummary.AverageRespect,
                thanksCoinSummary.RespectDensity,
                challengeAcceptanceScore,
                project.CrossDomainExposure)
            : intellectualRespectSummary;

        knowledgePoint.RespectDiversityIndex = intellectualRespectDerived.IntellectualRespectDiversityIndex;
        knowledgePoint.IdeaAcceptanceScore = intellectualRespectDerived.IdeaAcceptanceScore;
        knowledgePoint.IntellectualRespectJson = intellectualRespectDerived.IntellectualRespectJson;
        knowledgePoint.AverageIntellectualRespect = intellectualRespectDerived.AverageIntellectualRespect;
        knowledgePoint.IntellectualRespectDensity = intellectualRespectDerived.IntellectualRespectDensity;
        knowledgePoint.IntellectualRespectStrongLinks = intellectualRespectDerived.IntellectualRespectStrongLinks;
        knowledgePoint.IntellectualRespectWeakLinks = intellectualRespectDerived.IntellectualRespectWeakLinks;
        knowledgePoint.IntellectualRespectConcentration = intellectualRespectDerived.IntellectualRespectConcentration;
        knowledgePoint.IntellectualRespectDiversityIndex = intellectualRespectDerived.IntellectualRespectDiversityIndex;
        knowledgePoint.MutualMentorshipScore = intellectualRespectDerived.MutualMentorshipScore;
        knowledgePoint.MentorshipLinkCount = intellectualRespectDerived.MentorshipLinkCount;
        knowledgePoint.CrossMentorshipLinkCount = intellectualRespectDerived.CrossMentorshipLinkCount;
        knowledgePoint.MentorshipDiversityIndex = intellectualRespectDerived.MentorshipDiversityIndex;
        knowledgePoint.LearningFromOthersScore = intellectualRespectDerived.LearningFromOthersScore;
        knowledgePoint.LearnedFromUnexpectedAgentCount = intellectualRespectDerived.LearnedFromUnexpectedAgentCount;
        knowledgePoint.IntellectualRespectIdeaAcceptanceComponent = intellectualRespectDerived.IntellectualRespectIdeaAcceptanceComponent;
        knowledgePoint.IntellectualRespectChallengeAcceptanceComponent = intellectualRespectDerived.IntellectualRespectChallengeAcceptanceComponent;
        knowledgePoint.IntellectualRespectReconfigurationComponent = intellectualRespectDerived.IntellectualRespectReconfigurationComponent;
        knowledgePoint.IntellectualRespectSerendipityComponent = intellectualRespectDerived.IntellectualRespectSerendipityComponent;
        knowledgePoint.IntellectualRespectEmergenceComponent = intellectualRespectDerived.IntellectualRespectEmergenceComponent;
        knowledgePoint.EgoPenaltyApplied = intellectualRespectDerived.EgoPenaltyApplied;
        knowledgePoint.HierarchyPenaltyApplied = intellectualRespectDerived.HierarchyPenaltyApplied;
        knowledgePoint.ThanksIdeaAsIntellectualRespectSignal = intellectualRespectDerived.ThanksIdeaAsIntellectualRespectSignal;
        knowledgePoint.ThanksChallengeAsIntellectualRespectSignal = intellectualRespectDerived.ThanksChallengeAsIntellectualRespectSignal;
        knowledgePoint.ThanksBridgeAsMentorshipSignal = intellectualRespectDerived.ThanksBridgeAsMentorshipSignal;

        knowledgePoint.KnowledgeRecombinationScore = Clamp01(
            knowledgePoint.KnowledgeRecombinationScore
            + (intellectualRespectDerived.IdeaAcceptanceScore * 0.05)
            + (intellectualRespectDerived.IntellectualRespectReconfigurationComponent * 0.08));
        knowledgePoint.KnowledgeReconfigurationScore = Clamp01(
            knowledgePoint.KnowledgeReconfigurationScore
            + intellectualRespectDerived.IntellectualRespectReconfigurationComponent
            + (intellectualRespectDerived.MutualMentorshipScore * 0.05));
        knowledgePoint.SerendipityScore = Clamp01(
            knowledgePoint.SerendipityScore
            + intellectualRespectDerived.IntellectualRespectSerendipityComponent
            + (intellectualRespectDerived.ThanksBridgeAsMentorshipSignal * 0.03));
        knowledgePoint.KnowledgeRewiringScore = Clamp01(
            knowledgePoint.KnowledgeRewiringScore
            + (intellectualRespectDerived.IntellectualRespectReconfigurationComponent * 0.05)
            + (intellectualRespectDerived.IntellectualRespectDiversityIndex * 0.03));

        var phaseDecision = await DeterminePhaseAsync(
            project,
            stepNo,
            currentStepActions,
            knowledgePoint,
            cancellationToken);
        project.Phase = phaseDecision.Phase;
        knowledgePoint.Phase = project.Phase;
        knowledgePoint.SelectedPhase = project.Phase;
        knowledgePoint.PhaseDecisionScore = phaseDecision.PhaseDecisionScore;
        knowledgePoint.EmergentScore = phaseDecision.EmergentScore;
        knowledgePoint.StableScore = phaseDecision.StableScore;
        knowledgePoint.LearningScore = phaseDecision.LearningScore;
        knowledgePoint.SiloScore = phaseDecision.SiloScore;
        knowledgePoint.ChaosScore = phaseDecision.ChaosScore;
        knowledgePoint.CollapseScore = phaseDecision.CollapseScore;
        knowledgePoint.AdaptationScore = phaseDecision.AdaptationScore;
        knowledgePoint.EmergentCriteriaJson = phaseDecision.EmergentCriteriaJson;
        knowledgePoint.PhaseDecisionReason = BuildPhaseDecisionReason(project.Phase, phaseDecision);
        knowledgePoint.SerendipityDrivenReconfiguration = DetermineSerendipityDrivenReconfiguration(previousKnowledgePoint, previousKnowledgeTimeline, knowledgePoint);
        knowledgePoint.SerendipityToEmergenceLink = DetermineSerendipityToEmergenceLink(previousKnowledgeTimeline, knowledgePoint, project.Phase);
        var pipelineBottleneck = KnowledgeAnalysisService.DeterminePipelineBottleneck(
            knowledgePoint.SerendipityScore,
            knowledgePoint.KnowledgeRecombinationScore,
            knowledgePoint.KnowledgeReconfigurationScore,
            knowledgePoint.LearningScore,
            knowledgePoint.AdaptationScore,
            knowledgePoint.EmergentScore);
        var pipelineBottleneckScore = KnowledgeAnalysisService.GetPipelineBottleneckScore(
            pipelineBottleneck,
            knowledgePoint.SerendipityScore,
            knowledgePoint.KnowledgeRecombinationScore,
            knowledgePoint.KnowledgeReconfigurationScore,
            knowledgePoint.LearningScore,
            knowledgePoint.AdaptationScore,
            knowledgePoint.EmergentScore);
        var pipelineCompletionScore = KnowledgeAnalysisService.CalculatePipelineCompletionScore(
            knowledgePoint.SerendipityScore,
            knowledgePoint.KnowledgeRecombinationScore,
            knowledgePoint.KnowledgeReconfigurationScore,
            knowledgePoint.LearningScore,
            knowledgePoint.AdaptationScore,
            knowledgePoint.EmergentScore);
        project.CurrentStep = stepNo;
        if (project.CurrentStep >= project.TotalSteps)
        {
            project.Status = SimulationStatus.Completed;
        }

        var stateJson = JsonSerializer.Serialize(new
        {
            project.Id,
            project.Name,
            StepNo = stepNo,
            project.Phase,
            selectedPhase = project.Phase,
            project.Purpose,
            project.BoundaryConditions,
            project.KpiDefinition,
            project.LlmProvider,
            project.LlmModel,
            project.InformationSharingLevel,
            project.CooperationLevel,
            project.CompetitionLevel,
            project.PsychologicalSafetyLevel,
            project.LearningOrientationLevel,
            project.CustomerOrientationLevel,
            project.ShortTermResultPressureLevel,
            project.RewiringSensitivity,
            project.EnableExternalShock,
            project.ShockStep,
            configuredShockType = project.ShockType,
            project.ShockDescription,
            project.EnableChallengeEvent,
            project.ChallengeStep,
            configuredChallengeType = project.ChallengeType,
            project.ChallengeLevel,
            project.ChallengeDescription,
            project.RequiredKnowledgeDiversity,
            project.RequiredCrossDomainExposure,
            project.RequiredRewiringScore,
            project.ExplorationTendency,
            project.SerendipitySensitivity,
            project.KnowledgeRecombinationRate,
            project.SerendipityThreshold,
            project.EnableSerendipity,
            project.EnableTrustDynamics,
            project.TrustGrowthRate,
            project.TrustDecayRate,
            project.TrustSaturationStrength,
            project.TrustCapacity,
            project.TrustCapacityPenalty,
            project.DistrustPenalty,
            project.ConstructiveCriticismBonus,
            thanksCoinOccurred = knowledgePoint.ThanksCoinOccurred,
            thanksCoinCount = knowledgePoint.ThanksCoinCount,
            thanksCoinHelpCount = knowledgePoint.ThanksCoinHelpCount,
            thanksCoinIdeaCount = knowledgePoint.ThanksCoinIdeaCount,
            thanksCoinChallengeCount = knowledgePoint.ThanksCoinChallengeCount,
            thanksCoinBridgeCount = knowledgePoint.ThanksCoinBridgeCount,
            thanksCoinRespectDelta = knowledgePoint.ThanksCoinRespectDelta,
            thanksCoinTrustDelta = knowledgePoint.ThanksCoinTrustDelta,
            thanksCoinReconfigurationDelta = knowledgePoint.ThanksCoinReconfigurationDelta,
            thanksCoinSerendipityDelta = knowledgePoint.ThanksCoinSerendipityDelta,
            thanksCoinBridgeDelta = knowledgePoint.ThanksCoinBridgeDelta,
            thanksConcentration = knowledgePoint.ThanksConcentration,
            challengeAcceptanceScore = knowledgePoint.ChallengeAcceptanceScore,
            respectReconfigurationBoost = knowledgePoint.RespectReconfigurationBoost,
            thanksChallengeEffect = knowledgePoint.ThanksChallengeEffect,
            thanksBridgeEffect = knowledgePoint.ThanksBridgeEffect,
            diversityRespectEffect = knowledgePoint.DiversityRespectEffect,
            respectEmergenceComponent = knowledgePoint.RespectEmergenceComponent,
            popularityTrapPenalty = knowledgePoint.PopularityTrapPenalty,
            popularityTrapDetected = knowledgePoint.PopularityTrapDetected,
            thanksCoinToReconfigurationContribution = knowledgePoint.ThanksCoinToReconfigurationContribution,
            thanksCoinToSerendipityContribution = knowledgePoint.ThanksCoinToSerendipityContribution,
            thanksCoinToEmergenceContribution = knowledgePoint.ThanksCoinToEmergenceContribution,
            averageRespect = knowledgePoint.AverageRespect,
            respectDensity = knowledgePoint.RespectDensity,
            respectStrongLinks = knowledgePoint.RespectStrongLinks,
            respectWeakLinks = knowledgePoint.RespectWeakLinks,
            respectConcentration = knowledgePoint.RespectConcentration,
            respectJson = knowledgePoint.RespectJson,
            respectDiversityIndex = knowledgePoint.RespectDiversityIndex,
            ideaAcceptanceScore = knowledgePoint.IdeaAcceptanceScore,
            intellectualRespectJson = knowledgePoint.IntellectualRespectJson,
            averageIntellectualRespect = knowledgePoint.AverageIntellectualRespect,
            intellectualRespectDensity = knowledgePoint.IntellectualRespectDensity,
            intellectualRespectStrongLinks = knowledgePoint.IntellectualRespectStrongLinks,
            intellectualRespectWeakLinks = knowledgePoint.IntellectualRespectWeakLinks,
            intellectualRespectConcentration = knowledgePoint.IntellectualRespectConcentration,
            intellectualRespectDiversityIndex = knowledgePoint.IntellectualRespectDiversityIndex,
            mutualMentorshipScore = knowledgePoint.MutualMentorshipScore,
            mentorshipLinkCount = knowledgePoint.MentorshipLinkCount,
            crossMentorshipLinkCount = knowledgePoint.CrossMentorshipLinkCount,
            mentorshipDiversityIndex = knowledgePoint.MentorshipDiversityIndex,
            learningFromOthersScore = knowledgePoint.LearningFromOthersScore,
            learnedFromUnexpectedAgentCount = knowledgePoint.LearnedFromUnexpectedAgentCount,
            intellectualRespectIdeaAcceptanceComponent = knowledgePoint.IntellectualRespectIdeaAcceptanceComponent,
            intellectualRespectChallengeAcceptanceComponent = knowledgePoint.IntellectualRespectChallengeAcceptanceComponent,
            intellectualRespectReconfigurationComponent = knowledgePoint.IntellectualRespectReconfigurationComponent,
            intellectualRespectSerendipityComponent = knowledgePoint.IntellectualRespectSerendipityComponent,
            intellectualRespectEmergenceComponent = knowledgePoint.IntellectualRespectEmergenceComponent,
            egoPenaltyApplied = knowledgePoint.EgoPenaltyApplied,
            hierarchyPenaltyApplied = knowledgePoint.HierarchyPenaltyApplied,
            thanksIdeaAsIntellectualRespectSignal = knowledgePoint.ThanksIdeaAsIntellectualRespectSignal,
            thanksChallengeAsIntellectualRespectSignal = knowledgePoint.ThanksChallengeAsIntellectualRespectSignal,
            thanksBridgeAsMentorshipSignal = knowledgePoint.ThanksBridgeAsMentorshipSignal,
            knowledgeStock = knowledgePoint.KnowledgeStock,
            knowledgeDiversity = knowledgePoint.KnowledgeDiversity,
            externalShockLevel = knowledgePoint.ExternalShockLevel,
            crossDomainExposure = knowledgePoint.CrossDomainExposure,
            knowledgeRewiringScore = knowledgePoint.KnowledgeRewiringScore,
            explorationScore = knowledgePoint.ExplorationScore,
            serendipityScore = knowledgePoint.SerendipityScore,
            serendipityOccurred = knowledgePoint.SerendipityOccurred,
            knowledgeRecombinationScore = knowledgePoint.KnowledgeRecombinationScore,
            knowledgeReconfigurationScore = knowledgePoint.KnowledgeReconfigurationScore,
            serendipityDrivenReconfiguration = knowledgePoint.SerendipityDrivenReconfiguration,
            serendipityToEmergenceLink = knowledgePoint.SerendipityToEmergenceLink,
            averageTrustDecayApplied = trustDynamicsSummary.AverageTrustDecayApplied,
            trustCapacityPenaltyAppliedCount = trustDynamicsSummary.TrustCapacityPenaltyAppliedCount,
            trustCapacityPenaltyTotal = trustDynamicsSummary.TrustCapacityPenaltyTotal,
            averageTrustSaturationEffect = trustDynamicsSummary.AverageTrustSaturationEffect,
            averageTrustGrowthRateEffective = trustDynamicsSummary.AverageTrustGrowthRateEffective,
            shockOccurred = knowledgePoint.ShockOccurred,
            shockType = knowledgePoint.ShockType,
            challengeOccurred = knowledgePoint.ChallengeOccurred,
            challengeActive = knowledgePoint.ChallengeActive,
            challengeResolved = knowledgePoint.ChallengeResolved,
            challengeType = knowledgePoint.ChallengeType,
            challengeResolutionScore = knowledgePoint.ChallengeResolutionScore,
            challengeGap = knowledgePoint.ChallengeGap,
            knowledgeInterpretation = knowledgePoint.Interpretation,
            phaseDecisionScore = knowledgePoint.PhaseDecisionScore,
            emergentScore = knowledgePoint.EmergentScore,
            stableScore = knowledgePoint.StableScore,
            learningScore = knowledgePoint.LearningScore,
            siloScore = knowledgePoint.SiloScore,
            chaosScore = knowledgePoint.ChaosScore,
            collapseScore = knowledgePoint.CollapseScore,
            adaptationScore = knowledgePoint.AdaptationScore,
            pipelineBottleneck,
            pipelineBottleneckScore,
            pipelineCompletionScore,
            emergentCriteriaJson = knowledgePoint.EmergentCriteriaJson,
            phaseDecisionReason = knowledgePoint.PhaseDecisionReason,
            Agents = project.Agents.Select(agent => new
            {
                agent.Id,
                agent.Name,
                agent.Role,
                agent.Personality,
                agent.Orientation,
                agent.Memory,
                agent.TrustJson,
                agent.PositionX,
                agent.PositionY
            })
        }, JsonOptions);

        var step = new SimulationStep
        {
            SimulationProjectId = project.Id,
            StepNo = stepNo,
            Phase = project.Phase,
            StateJson = stateJson,
            CreatedAt = DateTime.UtcNow
        };

        var shouldPersistStep = ShouldPersistStep(project, stepNo);
        var shouldPersistTrustSnapshots = ShouldPersistTrustSnapshots(project, stepNo);
        if (shouldPersistTrustSnapshots)
        {
            await SaveTrustSnapshotsAsync(project, stepNo, cancellationToken);
        }

        if (shouldPersistStep)
        {
            db.SimulationSteps.Add(step);
        }
        await db.SaveChangesAsync(cancellationToken);
        return step;
    }

    private async Task<LlmAgentResponse> CompleteAgentTurnAsync(
        SimulationProject project,
        string prompt,
        CancellationToken cancellationToken)
    {
        var provider = string.IsNullOrWhiteSpace(project.LlmProvider)
            ? LlmDefaults.Provider
            : project.LlmProvider.Trim();
        var model = string.IsNullOrWhiteSpace(project.LlmModel)
            ? GetDefaultModel(provider)
            : project.LlmModel.Trim();

        if (string.Equals(provider, LlmProviderType.OpenAI, StringComparison.OrdinalIgnoreCase))
        {
            var apiKey = Environment.GetEnvironmentVariable("ORGSIM_OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return await openAiLlmService.CompleteAgentTurnAsync(prompt, model, cancellationToken);
            }

            var fallback = await mockLlmService.CompleteAgentTurnAsync(prompt, model, cancellationToken);
            fallback.RawResponse = $"[Fallback: requested provider=OpenAI, model={model}, reason=ORGSIM_OPENAI_API_KEY is not set] {fallback.RawResponse}";
            return fallback;
        }

        return await mockLlmService.CompleteAgentTurnAsync(prompt, model, cancellationToken);
    }

    public async Task<SimulationProject?> RunAllAsync(int simulationId, CancellationToken cancellationToken = default)
        => await RunAllAsync(simulationId, FullPersistenceMode, cancellationToken);

    public async Task<SimulationProject?> RunAllAsync(int simulationId, string persistenceMode, CancellationToken cancellationToken = default)
    {
        var normalizedMode = NormalizePersistenceMode(persistenceMode);
        var project = await db.SimulationProjects.FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        ApplyLogRetentionMode(project, normalizedMode);
        await db.SaveChangesAsync(cancellationToken);

        if (project.CurrentStep >= project.TotalSteps)
        {
            if (!string.Equals(project.Status, SimulationStatus.Failed, StringComparison.OrdinalIgnoreCase))
            {
                project.CurrentStep = Math.Max(project.CurrentStep, project.TotalSteps);
                project.Status = SimulationStatus.Completed;
                await db.SaveChangesAsync(cancellationToken);
            }

            await SaveMetricsAsync(simulationId, cancellationToken);

            if (!string.Equals(normalizedMode, FullPersistenceMode, StringComparison.OrdinalIgnoreCase))
            {
                var completedProject = await db.SimulationProjects.FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);
                var finalStepNo = completedProject?.CurrentStep ?? 0;
                if (finalStepNo > 0)
                {
                    await PrunePersistedHistoryAsync(simulationId, finalStepNo, normalizedMode, true, cancellationToken);
                }
            }

            return await db.SimulationProjects
                .Include(item => item.Metrics)
                .FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);
        }

        do
        {
            var step = await RunOneStepAsync(simulationId, cancellationToken);
            if (step is null)
            {
                break;
            }

            project = await db.SimulationProjects.FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);
            if (!string.Equals(normalizedMode, FullPersistenceMode, StringComparison.OrdinalIgnoreCase)
                && project is not null
                && step.StepNo < project.TotalSteps)
            {
                await PrunePersistedHistoryAsync(simulationId, step.StepNo, normalizedMode, false, cancellationToken);
            }
        }
        while (project is not null && project.CurrentStep < project.TotalSteps);

        await SaveMetricsAsync(simulationId, cancellationToken);

        if (!string.Equals(normalizedMode, FullPersistenceMode, StringComparison.OrdinalIgnoreCase))
        {
            var completedProject = await db.SimulationProjects.FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);
            var finalStepNo = completedProject?.CurrentStep ?? 0;
            if (finalStepNo > 0)
            {
                await PrunePersistedHistoryAsync(simulationId, finalStepNo, normalizedMode, true, cancellationToken);
            }
        }

        return await db.SimulationProjects
            .Include(item => item.Metrics)
            .FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);
    }

    private static string BuildPrompt(SimulationProject project, Agent agent, IReadOnlyCollection<AgentAction> recentActions)
    {
        var recent = recentActions.Count == 0
            ? "No previous messages."
            : string.Join("\n", recentActions.Select(action => $"- {action.Agent?.Name ?? "Unknown"}: {action.Message} (action: {action.Action})"));
        var roster = string.Join(", ", project.Agents.Where(item => item.Id != agent.Id).OrderBy(item => item.Id).Select(item => item.Name));
        if (string.IsNullOrWhiteSpace(roster))
        {
            roster = "None";
        }

        return $$"""
        Simulation purpose:
        {{project.Purpose}}

        Culture and boundary conditions:
        {{project.BoundaryConditions}}

        KPI:
        {{project.KpiDefinition}}

        You are:
        Name: {{agent.Name}}
        Role: {{agent.Role}}
        Memory: {{agent.Memory}}
        Your personality: {{agent.Personality}}
        Your orientation: {{agent.Orientation}}

        Personality guidance:
        - Conservative: prefer safe and proven actions, avoid unnecessary risk.
        - Challenger: actively propose new ideas and challenge assumptions.
        - Coordinator: connect agents and align discussions.
        - Critic: identify risks, contradictions, and weak assumptions constructively.
        - Supporter: support others and maintain trust.
        - Analyst: organize information and evaluate evidence.

        Orientation guidance:
        - CustomerFocused: prioritize customer success and market response.
        - FieldFocused: prioritize practical operation and real-world constraints.
        - QualityFocused: prioritize quality, reproducibility, and reliability.
        - SpeedFocused: prioritize fast execution and decision speed.
        - CostFocused: prioritize cost, efficiency, and resource constraints.
        - LearningFocused: prioritize feedback loops and organizational learning.

        Boundary condition parameters:
        - InformationSharingLevel: {{project.InformationSharingLevel.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}}
        - CooperationLevel: {{project.CooperationLevel.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}}
        - CompetitionLevel: {{project.CompetitionLevel.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}}
        - PsychologicalSafetyLevel: {{project.PsychologicalSafetyLevel.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}}
        - LearningOrientationLevel: {{project.LearningOrientationLevel.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}}
        - CustomerOrientationLevel: {{project.CustomerOrientationLevel.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}}
        - ShortTermResultPressureLevel: {{project.ShortTermResultPressureLevel.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}}

        Knowledge parameters:
        - KnowledgeStock: {{project.KnowledgeStock.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - KnowledgeDiversity: {{project.KnowledgeDiversity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - ExternalShockLevel: {{project.ExternalShockLevel.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - CrossDomainExposure: {{project.CrossDomainExposure.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - RewiringSensitivity: {{project.RewiringSensitivity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}

        External shock settings:
        - EnableExternalShock: {{project.EnableExternalShock}}
        - ShockStep: {{project.ShockStep}}
        - ShockType: {{project.ShockType}}
        - ShockDescription: {{project.ShockDescription}}

        Challenge settings:
        - ChallengeActive: {{(project.EnableChallengeEvent && project.ChallengeStep > 0 && (project.CurrentStep + 1) >= project.ChallengeStep).ToString()}}
        - EnableChallengeEvent: {{project.EnableChallengeEvent}}
        - ChallengeStep: {{project.ChallengeStep}}
        - ChallengeType: {{project.ChallengeType}}
        - ChallengeLevel: {{project.ChallengeLevel.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - ChallengeDescription: {{project.ChallengeDescription}}
        - RequiredKnowledgeDiversity: {{project.RequiredKnowledgeDiversity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - RequiredCrossDomainExposure: {{project.RequiredCrossDomainExposure.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - RequiredRewiringScore: {{project.RequiredRewiringScore.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}

        Serendipity settings:
        - EnableSerendipity: {{project.EnableSerendipity}}
        - ExplorationTendency: {{project.ExplorationTendency.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - SerendipitySensitivity: {{project.SerendipitySensitivity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - KnowledgeRecombinationRate: {{project.KnowledgeRecombinationRate.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}
        - SerendipityThreshold: {{project.SerendipityThreshold.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}}

        Other agents:
        {{roster}}

        Recent messages from the previous step:
        {{recent}}

        Available actions:
        {{string.Join(", ", AgentActionType.All)}}

        Return JSON only in this exact shape:
        {
          "message": "what you say to the organization",
          "memory": "your updated memory",
          "action": "one available action",
          "targetAgentName": "agent name or empty string"
        }
        """;
    }

    private async Task<PhaseDecisionResult> DeterminePhaseAsync(
        SimulationProject project,
        int stepNo,
        IReadOnlyCollection<AgentAction> currentStepActions,
        KnowledgeTimelinePoint knowledgePoint,
        CancellationToken cancellationToken)
    {
        if (stepNo <= 2)
        {
            return new PhaseDecisionResult
            {
                Phase = SimulationPhase.Forming,
                PhaseDecisionScore = 0,
                EmergentScore = 0,
                StableScore = 0,
                LearningScore = 0,
                SiloScore = 0,
                ChaosScore = 0,
                CollapseScore = 0,
                AdaptationScore = 0,
                EmergentCriteria = new Dictionary<string, EmergentCriterionState>(StringComparer.OrdinalIgnoreCase),
                PhaseDecisionReason = "初期ステップのため Forming と判定しました。"
            };
        }

        var minStep = Math.Max(1, stepNo - (PhaseWindowSize - 1));
        var recentActions = await db.AgentActions
            .Where(action => action.SimulationProjectId == project.Id && action.StepNo >= minStep && action.StepNo <= stepNo)
            .ToListAsync(cancellationToken);
        recentActions.AddRange(currentStepActions);

        if (recentActions.Count == 0)
        {
            return new PhaseDecisionResult
            {
                Phase = SimulationPhase.Forming,
                PhaseDecisionScore = 0,
                EmergentScore = 0,
                StableScore = 0,
                LearningScore = 0,
                SiloScore = 0,
                ChaosScore = 0,
                CollapseScore = 0,
                AdaptationScore = 0,
                EmergentCriteria = new Dictionary<string, EmergentCriterionState>(StringComparer.OrdinalIgnoreCase),
                PhaseDecisionReason = "行動データが不足しているため Forming と判定しました。"
            };
        }

        var recentSteps = await db.SimulationSteps
            .Where(step => step.SimulationProjectId == project.Id && step.StepNo >= minStep && step.StepNo < stepNo)
            .OrderBy(step => step.StepNo)
            .ToListAsync(cancellationToken);

        var counts = AgentActionType.All.ToDictionary(
            action => action,
            action => recentActions.Count(item => item.Action == action));

        var total = recentActions.Count;
        double Ratio(string action) => counts[action] / (double)total;
        var collaborationRatio = (counts[AgentActionType.ShareInfo] + counts[AgentActionType.AskHelp] + counts[AgentActionType.SupportOther]) / (double)total;
        var askHelpRatio = Ratio(AgentActionType.AskHelp);
        var ideaRatio = Ratio(AgentActionType.ProposeIdea);
        var criticismRatio = Ratio(AgentActionType.Criticize);
        var soloRatio = Ratio(AgentActionType.WorkAlone);
        var waitRatio = Ratio(AgentActionType.Wait);
        var shareRatio = Ratio(AgentActionType.ShareInfo);
        var supportRatio = Ratio(AgentActionType.SupportOther);
        var actionConcentration = counts.Values.Count == 0 ? 0 : counts.Values.Max() / (double)total;
        var phaseStabilityLocal = CalculateRecentPhaseStability(recentSteps);
        var networkMetrics = NetworkMetricsCalculator.CalculateFromAgents(project.Agents, project.EffectiveTrustThreshold);
        var trustComponent = Clamp01((networkMetrics.AverageTrust - 0.15) / 0.35);
        var densityComponent = Clamp01((networkMetrics.EffectiveNetworkDensity - 0.15) / 0.35);
        var strongLinkComponent = Clamp01(networkMetrics.StrongLinkCount / (double)Math.Max(project.Agents.Count * 2, 1));
        var diversityComponent = Clamp01((knowledgePoint.KnowledgeDiversity - 0.40) / 0.30);
        var recombinationComponent = Clamp01((knowledgePoint.KnowledgeRecombinationScore - 0.20) / 0.30);
        var reconfigurationComponent = Clamp01((knowledgePoint.KnowledgeReconfigurationScore - 0.20) / 0.30);
        var serendipityComponent = knowledgePoint.SerendipityOccurred ? 1.0 : Clamp01(knowledgePoint.SerendipityScore / 0.35);
        var proposeComponent = Clamp01((ideaRatio - 0.07) / 0.10);
        var shareComponent = Clamp01((shareRatio - 0.20) / 0.15);
        var thanksCoinRate = knowledgePoint.ThanksCoinCount <= 0 ? 0 : knowledgePoint.ThanksCoinCount / (double)Math.Max(project.Agents.Count, 1);
        var thanksChallengeRate = knowledgePoint.ThanksCoinCount <= 0 ? 0 : knowledgePoint.ThanksCoinChallengeCount / (double)knowledgePoint.ThanksCoinCount;
        var thanksBridgeRate = knowledgePoint.ThanksCoinCount <= 0 ? 0 : knowledgePoint.ThanksCoinBridgeCount / (double)knowledgePoint.ThanksCoinCount;
        var respectComponent = Clamp01((knowledgePoint.AverageRespect - 0.25) / 0.35);
        var respectDensityComponent = Clamp01((knowledgePoint.RespectDensity - 0.20) / 0.35);
        var respectConcentrationComponent = Clamp01(knowledgePoint.RespectConcentration);
        var intellectualRespectComponent = Clamp01((knowledgePoint.AverageIntellectualRespect - 0.25) / 0.35);
        var intellectualRespectDensityComponent = Clamp01((knowledgePoint.IntellectualRespectDensity - 0.20) / 0.35);
        var intellectualRespectDiversityComponent = Clamp01(knowledgePoint.IntellectualRespectDiversityIndex);
        var ideaAcceptanceComponent = Clamp01((knowledgePoint.IdeaAcceptanceScore - 0.35) / 0.35);
        var mutualMentorshipComponent = Clamp01(knowledgePoint.MutualMentorshipScore);
        var intellectualRespectReconfigurationComponent = Clamp01(knowledgePoint.IntellectualRespectReconfigurationComponent);
        var intellectualRespectSerendipityComponent = Clamp01(knowledgePoint.IntellectualRespectSerendipityComponent);
        var intellectualRespectEmergenceComponent = Clamp01(knowledgePoint.IntellectualRespectEmergenceComponent);
        var challengeAcceptanceComponent = Clamp01((knowledgePoint.ChallengeAcceptanceScore - 0.35) / 0.30);
        var respectReconfigurationComponent = Clamp01((knowledgePoint.RespectReconfigurationBoost - 0.10) / 0.20);
        var respectEmergenceComponent = Clamp01(knowledgePoint.RespectEmergenceComponent);
        var popularityTrapPenaltyComponent = Clamp01(knowledgePoint.PopularityTrapPenalty);
        var thanksContributionComponent = Clamp01(knowledgePoint.ThanksCoinToEmergenceContribution);
        var thanksChallengeComponent = Clamp01((thanksChallengeRate - 0.10) / 0.20);
        var thanksBridgeComponent = Clamp01((thanksBridgeRate - 0.10) / 0.20);
        var collapseScore = Clamp01((waitRatio * 0.45) + ((1 - ideaRatio) * 0.20) + ((1 - shareRatio) * 0.20) + ((1 - collaborationRatio) * 0.15));
        var chaosScore = Clamp01((criticismRatio * 0.30) + ((1 - supportRatio) * 0.20) + ((1 - networkMetrics.EffectiveNetworkDensity) * 0.10) + ((project.PsychologicalSafetyLevel < 0.60 ? 1.0 : 0.0) * 0.10) + ((1 - knowledgePoint.KnowledgeRewiringScore) * 0.30));
        var siloScore = Clamp01((soloRatio * 0.35) + ((1 - shareRatio) * 0.15) + ((1 - supportRatio) * 0.15) + ((1 - networkMetrics.EffectiveNetworkDensity) * 0.20) + ((1 - knowledgePoint.KnowledgeRewiringScore) * 0.10) + (Math.Max(knowledgePoint.ChallengeGap, 0) * 0.05));
        var adaptationScore = Clamp01((knowledgePoint.ExplorationScore * 0.25) + (knowledgePoint.KnowledgeRecombinationScore * 0.25) + (knowledgePoint.KnowledgeReconfigurationScore * 0.25) + ((shareRatio + supportRatio + proposeComponent) / 3.0 * 0.25));
        var emergentScore = Clamp01(
            (trustComponent * 0.12)
            + (densityComponent * 0.12)
            + (strongLinkComponent * 0.08)
            + (diversityComponent * 0.12)
            + (recombinationComponent * 0.13)
            + (reconfigurationComponent * 0.10)
            + (serendipityComponent * 0.08)
            + (proposeComponent * 0.06)
            + (shareComponent * 0.04)
            + (Clamp01(thanksCoinRate / 0.20) * 0.02)
            + (respectComponent * 0.05)
            + (respectDensityComponent * 0.05)
            + (intellectualRespectComponent * 0.05)
            + (intellectualRespectDensityComponent * 0.04)
            + (intellectualRespectDiversityComponent * 0.04)
            + (ideaAcceptanceComponent * 0.04)
            + (mutualMentorshipComponent * 0.05)
            + (intellectualRespectReconfigurationComponent * 0.05)
            + (intellectualRespectSerendipityComponent * 0.05)
            + (intellectualRespectEmergenceComponent * 0.04)
            + (challengeAcceptanceComponent * 0.08)
            + (respectReconfigurationComponent * 0.06)
            + (respectEmergenceComponent * 0.08)
            + (thanksChallengeComponent * 0.03)
            + (thanksBridgeComponent * 0.03)
            + (thanksContributionComponent * 0.06)
            - (popularityTrapPenaltyComponent * 0.12));
        var stableScore = Clamp01(
            (trustComponent * 0.26)
            + (densityComponent * 0.26)
            + ((1 - actionConcentration) * 0.16)
            + (phaseStabilityLocal * 0.14)
            + ((knowledgePoint.AverageRespect > 0.80 && knowledgePoint.ThanksConcentration > 0.60) ? 0.10 : 0)
            + (respectConcentrationComponent * 0.08)
            + (popularityTrapPenaltyComponent * 0.10));
        var learningScore = Clamp01((collaborationRatio * 0.35) + (knowledgePoint.KnowledgeStock * 0.25) + (shareRatio * 0.20) + (knowledgePoint.KnowledgeDiversity * 0.10) + ((1 - knowledgePoint.KnowledgeRecombinationScore) * 0.05) + ((1 - knowledgePoint.SerendipityScore) * 0.05));

        var emergentCriteria = BuildEmergentCriteria(project, knowledgePoint, networkMetrics, ideaRatio, shareRatio, thanksChallengeRate, thanksBridgeRate);
        var emergentCriteriaMetCount = emergentCriteria.Count(item => item.Value.Passed);

        if (collapseScore >= 0.60)
        {
            return new PhaseDecisionResult
            {
                Phase = SimulationPhase.Collapse,
                PhaseDecisionScore = collapseScore,
                EmergentScore = emergentScore,
                StableScore = stableScore,
                LearningScore = learningScore,
                SiloScore = siloScore,
                ChaosScore = chaosScore,
                CollapseScore = collapseScore,
                AdaptationScore = adaptationScore,
                EmergentCriteria = emergentCriteria,
                PhaseDecisionReason = "Collapse判定: 待機と情報不足が支配的なため崩壊相と判定しました。"
            };
        }

        if (chaosScore >= 0.60)
        {
            return new PhaseDecisionResult
            {
                Phase = SimulationPhase.Chaos,
                PhaseDecisionScore = chaosScore,
                EmergentScore = emergentScore,
                StableScore = stableScore,
                LearningScore = learningScore,
                SiloScore = siloScore,
                ChaosScore = chaosScore,
                CollapseScore = collapseScore,
                AdaptationScore = adaptationScore,
                EmergentCriteria = emergentCriteria,
                PhaseDecisionReason = "Chaos判定: 批判優勢とネットワーク不安定化が強いため混乱相と判定しました。"
            };
        }

        if (siloScore >= 0.60
            || (knowledgePoint.ChallengeActive && knowledgePoint.ChallengeGap > 0.15 && soloRatio >= 0.30 && supportRatio <= 0.10)
            || (soloRatio >= 0.35 && shareRatio <= 0.15 && knowledgePoint.KnowledgeRewiringScore < 0.35))
        {
            return new PhaseDecisionResult
            {
                Phase = SimulationPhase.Silo,
                PhaseDecisionScore = siloScore,
                EmergentScore = emergentScore,
                StableScore = stableScore,
                LearningScore = learningScore,
                SiloScore = siloScore,
                ChaosScore = chaosScore,
                CollapseScore = collapseScore,
                AdaptationScore = adaptationScore,
                EmergentCriteria = emergentCriteria,
                PhaseDecisionReason = "Silo判定: 単独作業と共有不足が支配的なためサイロ相と判定しました。"
            };
        }

        if ((knowledgePoint.ChallengeActive || knowledgePoint.ChallengeOccurred)
            && !knowledgePoint.ChallengeResolved
            && knowledgePoint.ExplorationScore >= 0.35
            && knowledgePoint.KnowledgeRecombinationScore >= 0.25
            && knowledgePoint.KnowledgeReconfigurationScore >= 0.25
            && (shareRatio + supportRatio + ideaRatio) >= 0.45
            && soloRatio < 0.35)
        {
            return new PhaseDecisionResult
            {
                Phase = SimulationPhase.Adaptation,
                PhaseDecisionScore = adaptationScore,
                EmergentScore = emergentScore,
                StableScore = stableScore,
                LearningScore = learningScore,
                SiloScore = siloScore,
                ChaosScore = chaosScore,
                CollapseScore = collapseScore,
                AdaptationScore = adaptationScore,
                EmergentCriteria = emergentCriteria,
                PhaseDecisionReason = "Adaptation判定: Challenge後に探索と再構成が進んでいるため適応相と判定しました。"
            };
        }

        if ((emergentScore >= 0.55 || emergentCriteriaMetCount >= 6)
            && siloScore < 0.55
            && chaosScore < 0.55
            && collapseScore < 0.55)
        {
            return new PhaseDecisionResult
            {
                Phase = SimulationPhase.Emergent,
                PhaseDecisionScore = emergentScore,
                EmergentScore = emergentScore,
                StableScore = stableScore,
                LearningScore = learningScore,
                SiloScore = siloScore,
                ChaosScore = chaosScore,
                CollapseScore = collapseScore,
                AdaptationScore = adaptationScore,
                EmergentCriteria = emergentCriteria,
                PhaseDecisionReason = $"Emergent判定: EmergentScoreが {emergentScore:0.000} のため創発相と判定しました。"
            };
        }

        if (stableScore >= 0.55
            && knowledgePoint.KnowledgeRecombinationScore < 0.30
            && knowledgePoint.SerendipityScore < 0.30
            && !knowledgePoint.ChallengeActive)
        {
            return new PhaseDecisionResult
            {
                Phase = SimulationPhase.Stable,
                PhaseDecisionScore = stableScore,
                EmergentScore = emergentScore,
                StableScore = stableScore,
                LearningScore = learningScore,
                SiloScore = siloScore,
                ChaosScore = chaosScore,
                CollapseScore = collapseScore,
                AdaptationScore = adaptationScore,
                EmergentCriteria = emergentCriteria,
                PhaseDecisionReason = "Stable判定: 信頼と密度は高いが、知識再結合と探索が弱いため安定相と判定しました。"
            };
        }

        if (learningScore >= 0.45 || collaborationRatio >= 0.45 || knowledgePoint.KnowledgeStock >= 0.35)
        {
            return new PhaseDecisionResult
            {
                Phase = SimulationPhase.Learning,
                PhaseDecisionScore = learningScore,
                EmergentScore = emergentScore,
                StableScore = stableScore,
                LearningScore = learningScore,
                SiloScore = siloScore,
                ChaosScore = chaosScore,
                CollapseScore = collapseScore,
                AdaptationScore = adaptationScore,
                EmergentCriteria = emergentCriteria,
                PhaseDecisionReason = "Learning判定: 知識共有と学習は進んでいますが、構造変化には届いていないため学習相と判定しました。"
            };
        }

        return new PhaseDecisionResult
        {
            Phase = SimulationPhase.Forming,
            PhaseDecisionScore = 0,
            EmergentScore = emergentScore,
            StableScore = stableScore,
            LearningScore = learningScore,
            SiloScore = siloScore,
            ChaosScore = chaosScore,
            CollapseScore = collapseScore,
            AdaptationScore = adaptationScore,
            EmergentCriteria = emergentCriteria,
            PhaseDecisionReason = "Forming判定: 初期形成段階に近いため形成相と判定しました。"
        };
    }
    private TrustChangeMetrics ApplyTrustUpdate(
        SimulationProject project,
        Agent actor,
        AgentAction agentAction,
        TrustDynamicsStepState trustDynamicsState,
        ISet<string> touchedTrustEdges)
    {
        var targetName = NormalizeTarget(agentAction.TargetAgentName);
        var targetAgent = project.Agents.FirstOrDefault(candidate =>
            candidate.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        var constructiveCriticism = project.PsychologicalSafetyLevel >= 0.7;
        List<TrustPairChange> actorSideChanges = [];

        switch (agentAction.Action)
        {
            case AgentActionType.SupportOther:
                if (targetAgent is not null)
                {
                    actorSideChanges.Add(ApplyTrustDelta(project, actor, targetAgent, 0.10, trustDynamicsState, touchedTrustEdges));
                    ApplyTrustDelta(project, targetAgent, actor, 0.15, trustDynamicsState, touchedTrustEdges);
                }
                break;

            case AgentActionType.AskHelp:
                if (targetAgent is not null)
                {
                    actorSideChanges.Add(ApplyTrustDelta(project, actor, targetAgent, 0.05, trustDynamicsState, touchedTrustEdges));
                    ApplyTrustDelta(project, targetAgent, actor, 0.05, trustDynamicsState, touchedTrustEdges);
                }
                break;

            case AgentActionType.ShareInfo:
                foreach (var otherAgent in project.Agents.Where(candidate => candidate.Id != actor.Id))
                {
                    actorSideChanges.Add(ApplyTrustDelta(project, actor, otherAgent, 0.03, trustDynamicsState, touchedTrustEdges));
                    ApplyTrustDelta(project, otherAgent, actor, 0.03, trustDynamicsState, touchedTrustEdges);
                }
                break;

            case AgentActionType.ProposeIdea:
                foreach (var otherAgent in project.Agents.Where(candidate => candidate.Id != actor.Id))
                {
                    actorSideChanges.Add(ApplyTrustDelta(project, actor, otherAgent, 0.02, trustDynamicsState, touchedTrustEdges));
                }
                break;

            case AgentActionType.Criticize:
                if (targetAgent is not null)
                {
                    var criticismDelta = GetCriticizeTrustDelta(project, constructiveCriticism);
                    actorSideChanges.Add(ApplyTrustDelta(project, actor, targetAgent, criticismDelta, trustDynamicsState, touchedTrustEdges));
                    ApplyTrustDelta(project, targetAgent, actor, criticismDelta, trustDynamicsState, touchedTrustEdges);
                }
                break;
        }

        return CalculateTrustMetrics(actorSideChanges);
    }

    private TrustDynamicsSummary ApplyTrustDynamics(
        SimulationProject project,
        ISet<string> touchedTrustEdges,
        TrustDynamicsStepState trustDynamicsState)
    {
        if (!project.EnableTrustDynamics || project.Agents.Count == 0)
        {
            return trustDynamicsState.ToSummary();
        }

        var totalPairs = 0;
        var totalDecayApplied = 0.0;

        foreach (var sourceAgent in project.Agents)
        {
            var trustMap = TrustJsonUtility.Deserialize(sourceAgent.TrustJson);
            foreach (var targetAgent in project.Agents.Where(candidate => candidate.Id != sourceAgent.Id))
            {
                totalPairs++;
                var key = GetTrustEdgeKey(sourceAgent.Name, targetAgent.Name);
                var before = trustMap.TryGetValue(targetAgent.Name, out var currentValue) ? currentValue : 0;
                var after = before;

                if (project.EnableTrustDynamics && project.TrustDecayRate > 0)
                {
                    after = ApplyDecayTowardZero(after, project.TrustDecayRate);
                    if (!touchedTrustEdges.Contains(key))
                    {
                        after = ApplyDecayTowardZero(after, project.TrustDecayRate * 0.5);
                    }
                }

                totalDecayApplied += Math.Abs(before - after);
                trustMap[targetAgent.Name] = after;
            }

            sourceAgent.TrustJson = JsonSerializer.Serialize(trustMap, JsonOptions);
        }

        var capacityPenaltyCount = 0;
        var capacityPenaltyTotal = 0.0;
        var capacityExceededAgentCount = 0;

        foreach (var sourceAgent in project.Agents)
        {
            var trustMap = TrustJsonUtility.Deserialize(sourceAgent.TrustJson);
            var stronglyTrustedTargets = project.Agents
                .Where(candidate => candidate.Id != sourceAgent.Id)
                .Select(candidate => new
                {
                    candidate.Name,
                    Trust = trustMap.TryGetValue(candidate.Name, out var trustValue) ? trustValue : 0
                })
                .Where(item => item.Trust >= 0.7)
                .OrderByDescending(item => item.Trust)
                .ThenBy(item => item.Name)
                .ToList();

            var trustCapacity = Math.Max(1, project.TrustCapacity);
            if (stronglyTrustedTargets.Count <= trustCapacity)
            {
                continue;
            }

            capacityExceededAgentCount++;
            foreach (var target in stronglyTrustedTargets.Skip(trustCapacity))
            {
                var before = trustMap.TryGetValue(target.Name, out var currentValue) ? currentValue : 0;
                var after = TrustJsonUtility.Clamp(before - Math.Clamp(project.TrustCapacityPenalty, 0, 1));
                trustMap[target.Name] = after;
                capacityPenaltyTotal += Math.Abs(before - after);
                if (Math.Abs(before - after) > 0)
                {
                    capacityPenaltyCount++;
                }
            }

            sourceAgent.TrustJson = JsonSerializer.Serialize(trustMap, JsonOptions);
        }

        trustDynamicsState.RecordDecay(totalDecayApplied, totalPairs);
        trustDynamicsState.RecordCapacityPenalty(capacityPenaltyCount, capacityPenaltyTotal, capacityExceededAgentCount);
        return trustDynamicsState.ToSummary();
    }

    private async Task SaveMetricsAsync(int simulationId, CancellationToken cancellationToken)
    {
        var project = await db.SimulationProjects
            .Include(item => item.Agents.OrderBy(agent => agent.Id))
            .Include(item => item.Metrics)
            .FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);

        if (project is null)
        {
            return;
        }

        var steps = await db.SimulationSteps
            .Where(step => step.SimulationProjectId == simulationId)
            .OrderBy(step => step.StepNo)
            .ToListAsync(cancellationToken);

        var actions = await db.AgentActions
            .Where(action => action.SimulationProjectId == simulationId)
            .OrderBy(action => action.StepNo)
            .ThenBy(action => action.AgentId)
            .ToListAsync(cancellationToken);

        var metrics = project.Metrics ?? new SimulationMetrics
        {
            SimulationProjectId = project.Id,
            CreatedAt = DateTime.UtcNow
        };

        var networkMetrics = NetworkMetricsCalculator.CalculateFromAgents(project.Agents, project.EffectiveTrustThreshold);

        metrics.FinalPhase = project.Phase;
        metrics.AverageTrust = networkMetrics.AverageTrust;
        metrics.NetworkDensity = networkMetrics.NetworkDensity;
        metrics.EffectiveNetworkDensity = networkMetrics.EffectiveNetworkDensity;
        metrics.StrongLinkCount = networkMetrics.StrongLinkCount;
        metrics.WeakLinkCount = networkMetrics.WeakLinkCount;
        metrics.ComponentCount = networkMetrics.ComponentCount;
        metrics.IsolatedAgentCount = networkMetrics.IsolatedCount;
        metrics.HubAgentName = networkMetrics.HubAgentName;
        metrics.HubScore = networkMetrics.HubScore;
        metrics.ShareInfoRate = CalculateActionRate(actions, AgentActionType.ShareInfo);
        metrics.ProposeIdeaRate = CalculateActionRate(actions, AgentActionType.ProposeIdea);
        metrics.CriticizeSupportRatio = CalculateCriticizeSupportRatio(actions);
        metrics.StepsToEmergent = FindFirstPhaseStep(steps, SimulationPhase.Emergent);
        metrics.StepsToLearning = FindFirstPhaseStep(steps, SimulationPhase.Learning);
        metrics.PhaseChangeCount = CalculatePhaseChangeCount(steps);
        metrics.PhaseStability = CalculatePhaseStability(steps, metrics.PhaseChangeCount);

        if (project.Metrics is null)
        {
            db.SimulationMetrics.Add(metrics);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveTrustSnapshotsAsync(
        SimulationProject project,
        int stepNo,
        CancellationToken cancellationToken)
    {
        var existingSnapshots = await db.TrustSnapshots
            .Where(snapshot => snapshot.SimulationProjectId == project.Id && snapshot.StepNo == stepNo)
            .ToListAsync(cancellationToken);

        if (existingSnapshots.Count > 0)
        {
            db.TrustSnapshots.RemoveRange(existingSnapshots);
        }

        List<TrustSnapshot> snapshots = [];
        var trustMaps = project.Agents.ToDictionary(
            agent => agent.Id,
            agent => TrustJsonUtility.Deserialize(agent.TrustJson));

        foreach (var sourceAgent in project.Agents)
        {
            foreach (var targetAgent in project.Agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                var trustMap = trustMaps[sourceAgent.Id];
                var trustValue = trustMap.TryGetValue(targetAgent.Name, out var value) ? value : 0;

                snapshots.Add(new TrustSnapshot
                {
                    SimulationProjectId = project.Id,
                    StepNo = stepNo,
                    SourceAgentId = sourceAgent.Id,
                    TargetAgentId = targetAgent.Id,
                    SourceAgentName = sourceAgent.Name,
                    TargetAgentName = targetAgent.Name,
                    TrustValue = TrustJsonUtility.Clamp(trustValue),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        db.TrustSnapshots.AddRange(snapshots);
    }

    private async Task PrunePersistedHistoryAsync(
        int simulationId,
        int currentStepNo,
        string persistenceMode,
        bool finalPass,
        CancellationToken cancellationToken)
    {
        if (string.Equals(persistenceMode, FullPersistenceMode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var stepNumbers = await db.SimulationSteps
            .Where(step => step.SimulationProjectId == simulationId)
            .Select(step => step.StepNo)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (stepNumbers.Count == 0)
        {
            return;
        }

        var keepStepNumbers = new HashSet<int>();
        var keepSnapshotStepNumbers = new HashSet<int>();
        var retainFromStepNo = Math.Max(1, currentStepNo - RetentionWindowStepCount + 1);

        foreach (var stepNo in stepNumbers)
        {
            if (!finalPass && stepNo >= retainFromStepNo)
            {
                keepStepNumbers.Add(stepNo);
                keepSnapshotStepNumbers.Add(stepNo);
                continue;
            }

            if (string.Equals(persistenceMode, SummaryPersistenceMode, StringComparison.OrdinalIgnoreCase)
                && stepNo % SummaryStepInterval == 0)
            {
                keepStepNumbers.Add(stepNo);
            }

            if (string.Equals(persistenceMode, SummaryPersistenceMode, StringComparison.OrdinalIgnoreCase)
                && stepNo % SummaryTrustSnapshotInterval == 0)
            {
                keepSnapshotStepNumbers.Add(stepNo);
            }

            if (finalPass)
            {
                if (string.Equals(persistenceMode, SummaryPersistenceMode, StringComparison.OrdinalIgnoreCase)
                    && (stepNo % SummaryStepInterval == 0 || stepNo == currentStepNo))
                {
                    keepStepNumbers.Add(stepNo);
                }
                else if (string.Equals(persistenceMode, MinimalPersistenceMode, StringComparison.OrdinalIgnoreCase)
                    && stepNo == currentStepNo)
                {
                    keepStepNumbers.Add(stepNo);
                }

                if (string.Equals(persistenceMode, SummaryPersistenceMode, StringComparison.OrdinalIgnoreCase)
                    && (stepNo % SummaryTrustSnapshotInterval == 0 || stepNo == currentStepNo))
                {
                    keepSnapshotStepNumbers.Add(stepNo);
                }
                else if (string.Equals(persistenceMode, MinimalPersistenceMode, StringComparison.OrdinalIgnoreCase)
                    && stepNo == currentStepNo)
                {
                    keepSnapshotStepNumbers.Add(stepNo);
                }
            }
        }

        var stepsToDelete = await db.SimulationSteps
            .Where(step => step.SimulationProjectId == simulationId && !keepStepNumbers.Contains(step.StepNo))
            .ToListAsync(cancellationToken);

        var actionsToDelete = await db.AgentActions
            .Where(action => action.SimulationProjectId == simulationId && !keepStepNumbers.Contains(action.StepNo))
            .ToListAsync(cancellationToken);

        var snapshotsToDelete = await db.TrustSnapshots
            .Where(snapshot => snapshot.SimulationProjectId == simulationId && !keepSnapshotStepNumbers.Contains(snapshot.StepNo))
            .ToListAsync(cancellationToken);

        if (stepsToDelete.Count > 0)
        {
            db.SimulationSteps.RemoveRange(stepsToDelete);
        }

        if (actionsToDelete.Count > 0)
        {
            db.AgentActions.RemoveRange(actionsToDelete);
        }

        if (snapshotsToDelete.Count > 0)
        {
            db.TrustSnapshots.RemoveRange(snapshotsToDelete);
        }

        if (stepsToDelete.Count > 0 || actionsToDelete.Count > 0 || snapshotsToDelete.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static string NormalizePersistenceMode(string? persistenceMode)
    {
        return string.IsNullOrWhiteSpace(persistenceMode)
            ? FullPersistenceMode
            : persistenceMode.Trim();
    }

    private static void ApplyLogRetentionMode(SimulationProject project, string persistenceMode)
    {
        var normalizedMode = NormalizeLogDetailLevel(persistenceMode);
        var currentMode = NormalizeLogDetailLevel(project.LogDetailLevel);
        var modeChanged = !string.Equals(normalizedMode, currentMode, StringComparison.OrdinalIgnoreCase);

        project.LogDetailLevel = normalizedMode;

        if (string.Equals(normalizedMode, LogDetailLevels.Full, StringComparison.OrdinalIgnoreCase))
        {
            project.StepLogInterval = LogRetentionDefaults.FullStepInterval;
            project.ActionLogInterval = LogRetentionDefaults.FullActionInterval;
            return;
        }

        if (modeChanged || project.StepLogInterval <= 0)
        {
            project.StepLogInterval = string.Equals(normalizedMode, LogDetailLevels.Minimal, StringComparison.OrdinalIgnoreCase)
                ? LogRetentionDefaults.MinimalStepInterval
                : LogRetentionDefaults.SummaryStepInterval;
        }

        if (modeChanged || project.ActionLogInterval <= 0)
        {
            project.ActionLogInterval = string.Equals(normalizedMode, LogDetailLevels.Minimal, StringComparison.OrdinalIgnoreCase)
                ? LogRetentionDefaults.MinimalActionInterval
                : LogRetentionDefaults.SummaryActionInterval;
        }
    }

    private static bool ShouldPersistStep(SimulationProject project, int stepNo)
    {
        var mode = NormalizeLogDetailLevel(project.LogDetailLevel);
        if (string.Equals(mode, LogDetailLevels.Full, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var interval = project.StepLogInterval > 0
            ? project.StepLogInterval
            : string.Equals(mode, LogDetailLevels.Minimal, StringComparison.OrdinalIgnoreCase)
                ? LogRetentionDefaults.MinimalStepInterval
                : LogRetentionDefaults.SummaryStepInterval;
        return stepNo >= project.TotalSteps || stepNo % interval == 0;
    }

    private static bool ShouldPersistActions(SimulationProject project, int stepNo)
    {
        var mode = NormalizeLogDetailLevel(project.LogDetailLevel);
        if (string.Equals(mode, LogDetailLevels.Full, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var interval = project.ActionLogInterval > 0
            ? project.ActionLogInterval
            : string.Equals(mode, LogDetailLevels.Minimal, StringComparison.OrdinalIgnoreCase)
                ? LogRetentionDefaults.MinimalActionInterval
                : LogRetentionDefaults.SummaryActionInterval;
        return stepNo >= project.TotalSteps || stepNo % interval == 0;
    }

    private static bool ShouldPersistTrustSnapshots(SimulationProject project, int stepNo)
    {
        var mode = NormalizeLogDetailLevel(project.LogDetailLevel);
        if (string.Equals(mode, LogDetailLevels.Full, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return stepNo >= project.TotalSteps;
    }

    private static string NormalizeLogDetailLevel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LogDetailLevels.Full;
        }

        var normalized = value.Trim();
        return LogDetailLevels.All.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? LogDetailLevels.All.First(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase))
            : LogDetailLevels.Full;
    }

    private static string NormalizeTarget(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Trim() == "-" ? "" : value.Trim();
    }

    private static string GetDefaultModel(string provider)
    {
        return string.Equals(provider, LlmProviderType.OpenAI, StringComparison.OrdinalIgnoreCase)
            ? LlmDefaults.OpenAiRecommendedModel
            : LlmDefaults.MockModel;
    }

    private static TrustPairChange ApplyTrustDelta(
        SimulationProject project,
        Agent sourceAgent,
        Agent targetAgent,
        double rawDelta,
        TrustDynamicsStepState trustDynamicsState,
        ISet<string> touchedTrustEdges)
    {
        if (sourceAgent.Id == targetAgent.Id)
        {
            return new TrustPairChange(null, null);
        }

        touchedTrustEdges.Add(GetTrustEdgeKey(sourceAgent.Name, targetAgent.Name));
        var trustMap = TrustJsonUtility.Deserialize(sourceAgent.TrustJson);
        var before = trustMap.TryGetValue(targetAgent.Name, out var currentValue) ? currentValue : 0;
        var delta = rawDelta;
        if (project.EnableTrustDynamics && rawDelta > 0)
        {
            var saturationEffect = Math.Clamp(Math.Clamp(project.TrustSaturationStrength, 0, 1) * Math.Max(before, 0), 0, 1);
            var growthMultiplier = Math.Clamp(project.TrustGrowthRate, 0, 2) * (1.0 - saturationEffect);
            delta = rawDelta * growthMultiplier;
            trustDynamicsState.RecordPositiveGrowth(growthMultiplier, saturationEffect);
        }

        var after = TrustJsonUtility.Clamp(before + delta);
        trustMap[targetAgent.Name] = after;
        sourceAgent.TrustJson = JsonSerializer.Serialize(trustMap, JsonOptions);
        return new TrustPairChange(before, after);
    }

    private static double ApplyDecayTowardZero(double trust, double decayRate)
    {
        if (trust > 0)
        {
            return TrustJsonUtility.Clamp(Math.Max(0, trust - decayRate));
        }

        if (trust < 0)
        {
            return TrustJsonUtility.Clamp(Math.Min(0, trust + decayRate));
        }

        return 0;
    }

    private static double GetCriticizeTrustDelta(SimulationProject project, bool constructiveCriticism)
    {
        if (constructiveCriticism && project.PsychologicalSafetyLevel >= 0.7)
        {
            return Math.Clamp(project.ConstructiveCriticismBonus, 0, 1);
        }

        if (project.PsychologicalSafetyLevel >= 0.4)
        {
            return -Math.Clamp(project.DistrustPenalty, 0, 1) * 0.5;
        }

        return -Math.Clamp(project.DistrustPenalty, 0, 1);
    }

    private static string GetTrustEdgeKey(string sourceAgentName, string targetAgentName)
    {
        return $"{sourceAgentName}=>{targetAgentName}";
    }

    private static TrustChangeMetrics CalculateTrustMetrics(IReadOnlyCollection<TrustPairChange> changes)
    {
        var appliedChanges = changes
            .Where(change => change.Before.HasValue && change.After.HasValue)
            .ToList();

        if (appliedChanges.Count == 0)
        {
            return new TrustChangeMetrics(null, null, null);
        }

        var before = Math.Round(appliedChanges.Average(change => change.Before!.Value), 2);
        var after = Math.Round(appliedChanges.Average(change => change.After!.Value), 2);
        var delta = Math.Round(after - before, 2);

        return new TrustChangeMetrics(before, delta, after);
    }

    private static double CalculateActionRate(IReadOnlyCollection<AgentAction> actions, string actionType)
    {
        if (actions.Count == 0)
        {
            return 0;
        }

        return Math.Round(actions.Count(action => action.Action == actionType) / (double)actions.Count, 3);
    }

    private static double CalculateCriticizeSupportRatio(IReadOnlyCollection<AgentAction> actions)
    {
        var criticizeCount = actions.Count(action => action.Action == AgentActionType.Criticize);
        var supportCount = actions.Count(action => action.Action == AgentActionType.SupportOther);

        if (supportCount == 0)
        {
            return criticizeCount == 0 ? 0 : criticizeCount;
        }

        return Math.Round(criticizeCount / (double)supportCount, 3);
    }

    private static int? FindFirstPhaseStep(IReadOnlyCollection<SimulationStep> steps, string phase)
    {
        return steps
            .OrderBy(step => step.StepNo)
            .FirstOrDefault(step => string.Equals(step.Phase, phase, StringComparison.OrdinalIgnoreCase))
            ?.StepNo;
    }

    private static int CalculatePhaseChangeCount(IReadOnlyList<SimulationStep> steps)
    {
        if (steps.Count <= 1)
        {
            return 0;
        }

        var count = 0;
        for (var index = 1; index < steps.Count; index++)
        {
            if (!string.Equals(steps[index - 1].Phase, steps[index].Phase, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private static double CalculatePhaseStability(IReadOnlyList<SimulationStep> steps, int phaseChangeCount)
    {
        var totalTransitions = Math.Max(0, steps.Count - 1);
        if (totalTransitions == 0)
        {
            return 1.0;
        }

        return Math.Round(1.0 - (phaseChangeCount / (double)totalTransitions), 3);
    }

    private static KnowledgeTimelinePoint ApplyKnowledgeShockAndChallenge(
        SimulationProject project,
        int stepNo,
        ActionDistributionSummary actions,
        KnowledgeTimelinePoint? previousKnowledgePoint)
    {
        var shockOccurred = project.EnableExternalShock
            && project.ShockStep > 0
            && project.ShockStep == stepNo
            && !string.Equals(project.ShockType, ShockTypes.None, StringComparison.OrdinalIgnoreCase);

        var effectiveExternalShockLevel = project.ExternalShockLevel;
        if (shockOccurred)
        {
            effectiveExternalShockLevel = Math.Clamp(project.ExternalShockLevel + 0.3, 0, 1);
            ApplyShockEffects(project);
        }

        var challengeOccurred = project.EnableChallengeEvent
            && project.ChallengeStep > 0
            && project.ChallengeStep == stepNo
            && !string.Equals(project.ChallengeType, ChallengeTypes.None, StringComparison.OrdinalIgnoreCase);
        var effectiveChallengeLevel = project.ChallengeLevel;
        if (challengeOccurred)
        {
            effectiveChallengeLevel = Math.Clamp(project.ChallengeLevel + 0.3, 0, 1);
            ApplyChallengeEffects(project);
        }

        var knowledgeStockDelta = KnowledgeAnalysisService.CalculateKnowledgeStockDelta(actions);
        var knowledgeDiversityDelta = KnowledgeAnalysisService.CalculateKnowledgeDiversityDelta(
            actions,
            project.PsychologicalSafetyLevel,
            project.CrossDomainExposure,
            effectiveExternalShockLevel,
            project.RewiringSensitivity);
        knowledgeDiversityDelta = Math.Round(knowledgeDiversityDelta + (effectiveChallengeLevel * 0.01), 4);

        project.KnowledgeStock = Math.Clamp(project.KnowledgeStock + knowledgeStockDelta, 0, 1);
        project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + knowledgeDiversityDelta, 0, 1);

        var rewiringScore = KnowledgeAnalysisService.CalculateKnowledgeRewiringScore(
            project.KnowledgeDiversity,
            project.CrossDomainExposure,
            effectiveExternalShockLevel,
            effectiveChallengeLevel,
            actions,
            project.PsychologicalSafetyLevel,
            project.RewiringSensitivity);
        var challengeWindowStarted = project.EnableChallengeEvent
            && project.ChallengeStep > 0
            && stepNo >= project.ChallengeStep
            && !string.Equals(project.ChallengeType, ChallengeTypes.None, StringComparison.OrdinalIgnoreCase);
        var challengeRequirementAverage = challengeWindowStarted
            ? KnowledgeAnalysisService.CalculateChallengeRequirementAverage(
                project.RequiredKnowledgeDiversity,
                project.RequiredCrossDomainExposure,
                project.RequiredRewiringScore)
            : 0;
        var provisionalChallengeGap = challengeWindowStarted
            ? Math.Round(Math.Max(
                challengeRequirementAverage - (previousKnowledgePoint?.ChallengeResolutionScore ?? 0),
                0), 4)
            : 0;
        var previousAverageRespect = previousKnowledgePoint?.AverageRespect ?? 0;
        var previousRespectDensity = previousKnowledgePoint?.RespectDensity ?? 0;
        var previousChallengeAcceptanceScore = previousKnowledgePoint?.ChallengeAcceptanceScore ?? 0;
        var previousThanksChallengeRate = previousKnowledgePoint is not null && previousKnowledgePoint.ThanksCoinCount > 0
            ? previousKnowledgePoint.ThanksCoinChallengeCount / (double)previousKnowledgePoint.ThanksCoinCount
            : 0;
        var previousThanksBridgeRate = previousKnowledgePoint is not null && previousKnowledgePoint.ThanksCoinCount > 0
            ? previousKnowledgePoint.ThanksCoinBridgeCount / (double)previousKnowledgePoint.ThanksCoinCount
            : 0;
        var explorationScore = project.EnableSerendipity
            ? SerendipityAnalysisService.CalculateExplorationScore(
                actions,
                project.CrossDomainExposure,
                project.ExplorationTendency,
                challengeWindowStarted,
                effectiveChallengeLevel)
            : 0;
        var serendipityScore = project.EnableSerendipity
            ? SerendipityAnalysisService.CalculateSerendipityScore(
                explorationScore,
                project.KnowledgeDiversity,
                project.CrossDomainExposure,
                provisionalChallengeGap,
                project.SerendipitySensitivity,
                actions.ProposeIdeaRate,
                challengeWindowStarted,
                effectiveChallengeLevel,
                project.PsychologicalSafetyLevel,
                actions.CriticizeRate,
                project.CompetitionLevel,
                actions.WorkAloneRate,
                previousAverageRespect,
                previousRespectDensity,
                previousChallengeAcceptanceScore,
                previousThanksChallengeRate,
                previousThanksBridgeRate)
            : 0;
        var serendipityOccurred = project.EnableSerendipity
            && serendipityScore >= Math.Clamp(project.SerendipityThreshold, 0, 1);
        var knowledgeRecombinationScore = project.EnableSerendipity
            ? SerendipityAnalysisService.CalculateKnowledgeRecombinationScore(
                serendipityScore,
                actions,
                project.PsychologicalSafetyLevel,
                project.KnowledgeRecombinationRate,
                project.ConstructiveCriticismBonus,
                previousAverageRespect,
                previousRespectDensity,
                previousChallengeAcceptanceScore,
                previousThanksChallengeRate,
                previousThanksBridgeRate)
            : 0;
        var knowledgeReconfigurationScore = KnowledgeAnalysisService.CalculateKnowledgeReconfigurationScore(
            rewiringScore,
            effectiveChallengeLevel,
            actions,
            project.PsychologicalSafetyLevel,
            challengeWindowStarted,
            knowledgeRecombinationScore,
            project.ConstructiveCriticismBonus,
            previousAverageRespect,
            previousRespectDensity,
            previousKnowledgePoint?.IdeaAcceptanceScore ?? 0,
            previousChallengeAcceptanceScore,
            previousKnowledgePoint?.AverageIntellectualRespect ?? 0,
            previousKnowledgePoint?.IntellectualRespectDensity ?? 0,
            previousKnowledgePoint?.MutualMentorshipScore ?? 0,
            previousThanksChallengeRate,
            previousThanksBridgeRate);
        var challengeResolutionScore = challengeWindowStarted
            ? KnowledgeAnalysisService.CalculateChallengeResolutionScore(
                project.KnowledgeDiversity,
                project.CrossDomainExposure,
                knowledgeReconfigurationScore,
                actions)
            : 0;
        var challengeResolved = challengeWindowStarted && challengeResolutionScore >= challengeRequirementAverage;
        var challengeActive = challengeWindowStarted && !challengeResolved;
        var challengeGap = challengeWindowStarted
            ? Math.Round(challengeRequirementAverage - challengeResolutionScore, 4)
            : 0;

        var interpretation = KnowledgeAnalysisService.BuildInterpretation(
            project.KnowledgeStock,
            project.KnowledgeDiversity,
            effectiveExternalShockLevel,
            project.CrossDomainExposure,
            rewiringScore,
            knowledgeReconfigurationScore,
            explorationScore,
            serendipityScore,
            knowledgeRecombinationScore,
            serendipityOccurred,
            challengeActive,
            challengeResolved,
            challengeGap,
            actions.WorkAloneRate,
            project.ConstructiveCriticismBonus);

        return new KnowledgeTimelinePoint
        {
            StepNo = stepNo,
            KnowledgeStock = Math.Round(project.KnowledgeStock, 4),
            KnowledgeDiversity = Math.Round(project.KnowledgeDiversity, 4),
            ExternalShockLevel = Math.Round(effectiveExternalShockLevel, 4),
            CrossDomainExposure = Math.Round(project.CrossDomainExposure, 4),
            KnowledgeRewiringScore = rewiringScore,
            ExplorationScore = explorationScore,
            SerendipityScore = serendipityScore,
            SerendipityOccurred = serendipityOccurred,
            KnowledgeRecombinationScore = knowledgeRecombinationScore,
            KnowledgeReconfigurationScore = knowledgeReconfigurationScore,
            SerendipityDrivenReconfiguration = previousKnowledgePoint is not null
                && serendipityOccurred
                && knowledgeReconfigurationScore > previousKnowledgePoint.KnowledgeReconfigurationScore,
            SerendipityToEmergenceLink = false,
            ShockOccurred = shockOccurred,
            ShockType = shockOccurred ? project.ShockType : ShockTypes.None,
            ChallengeOccurred = challengeOccurred,
            ChallengeActive = challengeActive,
            ChallengeResolved = challengeResolved,
            ChallengeType = challengeWindowStarted ? project.ChallengeType : ChallengeTypes.None,
            ChallengeResolutionScore = challengeResolutionScore,
            ChallengeGap = challengeGap,
            Interpretation = interpretation
        };
    }

    private static bool DetermineSerendipityDrivenReconfiguration(
        KnowledgeTimelinePoint? previousKnowledgePoint,
        IReadOnlyCollection<KnowledgeTimelinePoint> previousKnowledgeTimeline,
        KnowledgeTimelinePoint currentKnowledgePoint)
    {
        var priorSerendipity = previousKnowledgeTimeline
            .Where(point => point.StepNo >= currentKnowledgePoint.StepNo - 2)
            .Any(point => point.SerendipityOccurred);
        var previousReconfiguration = previousKnowledgePoint?.KnowledgeReconfigurationScore ?? 0;

        return (currentKnowledgePoint.SerendipityOccurred || priorSerendipity)
            && currentKnowledgePoint.KnowledgeReconfigurationScore > previousReconfiguration + 0.03;
    }

    private static bool DetermineSerendipityToEmergenceLink(
        IReadOnlyCollection<KnowledgeTimelinePoint> previousKnowledgeTimeline,
        KnowledgeTimelinePoint currentKnowledgePoint,
        string currentPhase)
    {
        if (!string.Equals(currentPhase, SimulationPhase.Adaptation, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(currentPhase, SimulationPhase.Emergent, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (currentKnowledgePoint.SerendipityOccurred)
        {
            return true;
        }

        return previousKnowledgeTimeline
            .Where(point => point.StepNo >= currentKnowledgePoint.StepNo - 3)
            .Any(point => point.SerendipityOccurred);
    }

    private static double CalculateRecentPhaseStability(IReadOnlyList<SimulationStep> recentSteps)
    {
        if (recentSteps.Count == 0)
        {
            return 1.0;
        }

        var ordered = recentSteps
            .OrderBy(step => step.StepNo)
            .TakeLast(3)
            .ToList();

        if (ordered.Count <= 1)
        {
            return 1.0;
        }

        var changes = 0;
        for (var index = 1; index < ordered.Count; index++)
        {
            if (!string.Equals(ordered[index - 1].Phase, ordered[index].Phase, StringComparison.OrdinalIgnoreCase))
            {
                changes++;
            }
        }

        return changes == 0 ? 1.0 : 0.5;
    }

    private static Dictionary<string, EmergentCriterionState> BuildEmergentCriteria(
        SimulationProject project,
        KnowledgeTimelinePoint knowledgePoint,
        NetworkMetricsResult networkMetrics,
        double ideaRatio,
        double shareRatio,
        double thanksChallengeRate,
        double thanksBridgeRate)
    {
        var strongLinkThreshold = Math.Max(project.Agents.Count * 2, 1);
        return new Dictionary<string, EmergentCriterionState>(StringComparer.OrdinalIgnoreCase)
        {
            ["AverageTrust"] = new EmergentCriterionState { Value = networkMetrics.AverageTrust, Threshold = 0.30, Passed = networkMetrics.AverageTrust >= 0.30 },
            ["EffectiveDensity"] = new EmergentCriterionState { Value = networkMetrics.EffectiveNetworkDensity, Threshold = 0.30, Passed = networkMetrics.EffectiveNetworkDensity >= 0.30 },
            ["StrongLinks"] = new EmergentCriterionState { Value = networkMetrics.StrongLinkCount, Threshold = strongLinkThreshold, Passed = networkMetrics.StrongLinkCount >= strongLinkThreshold },
            ["KnowledgeDiversity"] = new EmergentCriterionState { Value = knowledgePoint.KnowledgeDiversity, Threshold = 0.60, Passed = knowledgePoint.KnowledgeDiversity >= 0.60 },
            ["KnowledgeRecombination"] = new EmergentCriterionState { Value = knowledgePoint.KnowledgeRecombinationScore, Threshold = 0.25, Passed = knowledgePoint.KnowledgeRecombinationScore >= 0.25 },
            ["KnowledgeReconfiguration"] = new EmergentCriterionState { Value = knowledgePoint.KnowledgeReconfigurationScore, Threshold = 0.25, Passed = knowledgePoint.KnowledgeReconfigurationScore >= 0.25 },
            ["Serendipity"] = new EmergentCriterionState { Value = knowledgePoint.SerendipityOccurred ? 1 : knowledgePoint.SerendipityScore, Threshold = 0.30, Passed = knowledgePoint.SerendipityOccurred || knowledgePoint.SerendipityScore >= 0.30 },
            ["ProposeIdea"] = new EmergentCriterionState { Value = ideaRatio, Threshold = 0.07, Passed = ideaRatio >= 0.07 },
            ["ShareInfo"] = new EmergentCriterionState { Value = shareRatio, Threshold = 0.30, Passed = shareRatio >= 0.30 },
            ["AverageRespect"] = new EmergentCriterionState { Value = knowledgePoint.AverageRespect, Threshold = 0.50, Passed = knowledgePoint.AverageRespect >= 0.50 },
            ["RespectDensity"] = new EmergentCriterionState { Value = knowledgePoint.RespectDensity, Threshold = 0.30, Passed = knowledgePoint.RespectDensity >= 0.30 },
            ["RespectDiversityIndex"] = new EmergentCriterionState { Value = knowledgePoint.RespectDiversityIndex, Threshold = 0.30, Passed = knowledgePoint.RespectDiversityIndex >= 0.30 },
            ["IdeaAcceptanceScore"] = new EmergentCriterionState { Value = knowledgePoint.IdeaAcceptanceScore, Threshold = 0.50, Passed = knowledgePoint.IdeaAcceptanceScore >= 0.50 },
            ["AverageIntellectualRespect"] = new EmergentCriterionState { Value = knowledgePoint.AverageIntellectualRespect, Threshold = 0.40, Passed = knowledgePoint.AverageIntellectualRespect >= 0.40 },
            ["IntellectualRespectDensity"] = new EmergentCriterionState { Value = knowledgePoint.IntellectualRespectDensity, Threshold = 0.30, Passed = knowledgePoint.IntellectualRespectDensity >= 0.30 },
            ["IntellectualRespectDiversityIndex"] = new EmergentCriterionState { Value = knowledgePoint.IntellectualRespectDiversityIndex, Threshold = 0.30, Passed = knowledgePoint.IntellectualRespectDiversityIndex >= 0.30 },
            ["MutualMentorshipScore"] = new EmergentCriterionState { Value = knowledgePoint.MutualMentorshipScore, Threshold = 0.40, Passed = knowledgePoint.MutualMentorshipScore >= 0.40 },
            ["IntellectualRespectReconfigurationComponent"] = new EmergentCriterionState { Value = knowledgePoint.IntellectualRespectReconfigurationComponent, Threshold = 0.20, Passed = knowledgePoint.IntellectualRespectReconfigurationComponent >= 0.20 },
            ["IntellectualRespectSerendipityComponent"] = new EmergentCriterionState { Value = knowledgePoint.IntellectualRespectSerendipityComponent, Threshold = 0.20, Passed = knowledgePoint.IntellectualRespectSerendipityComponent >= 0.20 },
            ["ChallengeAcceptanceScore"] = new EmergentCriterionState { Value = knowledgePoint.ChallengeAcceptanceScore, Threshold = 0.55, Passed = knowledgePoint.ChallengeAcceptanceScore >= 0.55 },
            ["ThanksChallenge"] = new EmergentCriterionState { Value = thanksChallengeRate, Threshold = 0.10, Passed = thanksChallengeRate > 0.10 },
            ["ThanksBridge"] = new EmergentCriterionState { Value = thanksBridgeRate, Threshold = 0.10, Passed = thanksBridgeRate > 0.10 },
            ["RespectReconfigurationBoost"] = new EmergentCriterionState { Value = knowledgePoint.RespectReconfigurationBoost, Threshold = 0.10, Passed = knowledgePoint.RespectReconfigurationBoost >= 0.10 },
            ["RespectEmergenceComponent"] = new EmergentCriterionState { Value = knowledgePoint.RespectEmergenceComponent, Threshold = 0.10, Passed = knowledgePoint.RespectEmergenceComponent >= 0.10 },
            ["PopularityTrapNotDetected"] = new EmergentCriterionState { Value = knowledgePoint.PopularityTrapDetected ? 0 : 1, Threshold = 1, Passed = !knowledgePoint.PopularityTrapDetected }
        };
    }

    private static string BuildPhaseDecisionReason(
        string phase,
        PhaseDecisionResult phaseDecision)
    {
        return phase switch
        {
            SimulationPhase.Collapse => "Collapse判定: 待機と情報不足が支配的なため崩壊相と判定しました。",
            SimulationPhase.Chaos => "Chaos判定: 批判優勢とネットワーク不安定化が強いため混乱相と判定しました。",
            SimulationPhase.Silo => "Silo判定: 単独作業と共有不足が支配的なためサイロ相と判定しました。",
            SimulationPhase.Adaptation => "Adaptation判定: Challenge後に探索と再構成が進んでいるため適応相と判定しました。",
            SimulationPhase.Emergent => $"Emergent判定: EmergentScoreが {phaseDecision.EmergentScore:0.000} のため創発相と判定しました。",
            SimulationPhase.Stable => "Stable判定: 信頼と密度は高いが、知識再結合と探索が弱いため安定相と判定しました。",
            SimulationPhase.Learning => "Learning判定: 知識共有と学習は進んでいますが、構造変化には届いていないため学習相と判定しました。",
            SimulationPhase.Forming => "Forming判定: 初期形成段階に近いため形成相と判定しました。",
            _ => string.IsNullOrWhiteSpace(phase) ? "-" : $"{phase} と判定しました。"
        };
    }

    private static double Clamp01(double value)
    {
        return Math.Clamp(value, 0, 1);
    }

    private static void ApplyShockEffects(SimulationProject project)
    {
        switch (project.ShockType)
        {
            case ShockTypes.CrossDomainExpert:
                project.CrossDomainExposure = Math.Clamp(project.CrossDomainExposure + 0.3, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.1, 0, 1);
                break;
            case ShockTypes.CustomerDemandShift:
                project.CustomerOrientationLevel = Math.Clamp(project.CustomerOrientationLevel + 0.2, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.05, 0, 1);
                break;
            case ShockTypes.NewTechnology:
                project.CrossDomainExposure = Math.Clamp(project.CrossDomainExposure + 0.2, 0, 1);
                project.KnowledgeStock = Math.Clamp(project.KnowledgeStock + 0.05, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.1, 0, 1);
                break;
            case ShockTypes.CompetitorMove:
                project.ShortTermResultPressureLevel = Math.Clamp(project.ShortTermResultPressureLevel + 0.2, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.05, 0, 1);
                break;
            case ShockTypes.FailureIncident:
                if (project.PsychologicalSafetyLevel >= 0.5)
                {
                    project.KnowledgeStock = Math.Clamp(project.KnowledgeStock + 0.1, 0, 1);
                }
                else
                {
                    project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity - 0.05, 0, 1);
                }
                break;
            case ShockTypes.CultureShock:
                project.CrossDomainExposure = Math.Clamp(project.CrossDomainExposure + 0.3, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.15, 0, 1);
                break;
        }
    }

    private static void ApplyChallengeEffects(SimulationProject project)
    {
        switch (project.ChallengeType)
        {
            case ChallengeTypes.ExistingMethodFailure:
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.05, 0, 1);
                break;
            case ChallengeTypes.NewMarketRequirement:
                project.CustomerOrientationLevel = Math.Clamp(project.CustomerOrientationLevel + 0.2, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.05, 0, 1);
                break;
            case ChallengeTypes.CrossFunctionalProblem:
                project.CrossDomainExposure = Math.Clamp(project.CrossDomainExposure + 0.2, 0, 1);
                project.CooperationLevel = Math.Clamp(project.CooperationLevel + 0.1, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.08, 0, 1);
                break;
            case ChallengeTypes.QualityCrisis:
                project.KnowledgeStock = Math.Clamp(project.KnowledgeStock + 0.05, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.04, 0, 1);
                break;
            case ChallengeTypes.TechnologyShift:
                project.CrossDomainExposure = Math.Clamp(project.CrossDomainExposure + 0.25, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.10, 0, 1);
                break;
            case ChallengeTypes.CustomerComplexityIncrease:
                project.CustomerOrientationLevel = Math.Clamp(project.CustomerOrientationLevel + 0.15, 0, 1);
                project.CrossDomainExposure = Math.Clamp(project.CrossDomainExposure + 0.10, 0, 1);
                project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + 0.08, 0, 1);
                break;
        }
    }

    private sealed class TrustDynamicsStepState
    {
        private double PositiveGrowthMultiplierSum { get; set; }
        private int PositiveGrowthCount { get; set; }
        private double SaturationEffectSum { get; set; }
        private int SaturationEffectCount { get; set; }
        private double TotalDecayApplied { get; set; }
        private int DecaySampleCount { get; set; }
        public int TrustCapacityPenaltyAppliedCount { get; private set; }
        public double TrustCapacityPenaltyTotal { get; private set; }
        public int TrustCapacityExceededAgentCount { get; private set; }

        public void RecordPositiveGrowth(double growthMultiplier, double saturationEffect)
        {
            PositiveGrowthMultiplierSum += Math.Clamp(growthMultiplier, 0, 2);
            PositiveGrowthCount++;
            SaturationEffectSum += Math.Clamp(saturationEffect, 0, 1);
            SaturationEffectCount++;
        }

        public void RecordDecay(double totalDecayApplied, int sampleCount)
        {
            TotalDecayApplied += Math.Max(0, totalDecayApplied);
            DecaySampleCount += Math.Max(0, sampleCount);
        }

        public void RecordCapacityPenalty(int appliedCount, double totalPenalty, int exceededAgentCount)
        {
            TrustCapacityPenaltyAppliedCount += Math.Max(0, appliedCount);
            TrustCapacityPenaltyTotal += Math.Max(0, totalPenalty);
            TrustCapacityExceededAgentCount += Math.Max(0, exceededAgentCount);
        }

        public TrustDynamicsSummary ToSummary()
        {
            var averageGrowthMultiplier = PositiveGrowthCount == 0
                ? 0
                : Math.Round(PositiveGrowthMultiplierSum / PositiveGrowthCount, 4);
            var averageSaturationEffect = SaturationEffectCount == 0
                ? 0
                : Math.Round(SaturationEffectSum / SaturationEffectCount, 4);
            var averageDecayApplied = DecaySampleCount == 0
                ? 0
                : Math.Round(TotalDecayApplied / DecaySampleCount, 4);

            return new TrustDynamicsSummary
            {
                AverageTrustGrowthRateEffective = averageGrowthMultiplier,
                AverageTrustDecayApplied = averageDecayApplied,
                TrustCapacityPenaltyAppliedCount = TrustCapacityPenaltyAppliedCount,
                TrustCapacityPenaltyTotal = Math.Round(TrustCapacityPenaltyTotal, 4),
                AverageTrustSaturationEffect = averageSaturationEffect,
                TrustCapacityExceededAgentCount = TrustCapacityExceededAgentCount
            };
        }
    }

    private sealed record TrustDynamicsSummary
    {
        public double AverageTrustGrowthRateEffective { get; init; }
        public double AverageTrustDecayApplied { get; init; }
        public int TrustCapacityPenaltyAppliedCount { get; init; }
        public double TrustCapacityPenaltyTotal { get; init; }
        public double AverageTrustSaturationEffect { get; init; }
        public int TrustCapacityExceededAgentCount { get; init; }
    }

    private sealed class EmergentCriterionState
    {
        public double Value { get; init; }
        public double Threshold { get; init; }
        public bool Passed { get; init; }
    }

    private sealed record TrustPairChange(double? Before, double? After);
    private sealed record TrustChangeMetrics(double? Before, double? Delta, double? After);

    private sealed class PhaseDecisionResult
    {
        public string Phase { get; init; } = SimulationPhase.Forming;
        public double PhaseDecisionScore { get; init; }
        public double EmergentScore { get; init; }
        public double StableScore { get; init; }
        public double LearningScore { get; init; }
        public double SiloScore { get; init; }
        public double ChaosScore { get; init; }
        public double CollapseScore { get; init; }
        public double AdaptationScore { get; init; }
        public Dictionary<string, EmergentCriterionState> EmergentCriteria { get; init; } = [];
        public string PhaseDecisionReason { get; init; } = "";
        public string EmergentCriteriaJson => JsonSerializer.Serialize(EmergentCriteria, JsonOptions);
    }
}
