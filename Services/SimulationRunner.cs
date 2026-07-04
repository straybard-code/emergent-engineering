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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<SimulationStep?> RunOneStepAsync(int simulationId, CancellationToken cancellationToken = default)
    {
        var project = await db.SimulationProjects
            .Include(item => item.Agents.OrderBy(agent => agent.Id))
            .FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);

        if (project is null || project.CurrentStep >= project.TotalSteps)
        {
            return null;
        }

        var stepNo = project.CurrentStep + 1;
        project.Status = SimulationStatus.Running;

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
            db.AgentActions.Add(agentAction);
        }

        var stepActionSummary = ActionDistributionCalculator.Calculate(currentStepActions.Select(action => action.Action));
        var knowledgePoint = ApplyKnowledgeShockAndChallenge(project, stepNo, stepActionSummary, previousKnowledgePoint);
        var trustDynamicsSummary = ApplyTrustDynamics(project, touchedTrustEdges, trustDynamicsState);

        project.Phase = await DeterminePhaseAsync(
            project,
            stepNo,
            currentStepActions,
            knowledgePoint,
            cancellationToken);
        knowledgePoint.Phase = project.Phase;
        knowledgePoint.SerendipityDrivenReconfiguration = DetermineSerendipityDrivenReconfiguration(previousKnowledgePoint, previousKnowledgeTimeline, knowledgePoint);
        knowledgePoint.SerendipityToEmergenceLink = DetermineSerendipityToEmergenceLink(previousKnowledgeTimeline, knowledgePoint, project.Phase);
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

        await SaveTrustSnapshotsAsync(project, stepNo, cancellationToken);
        db.SimulationSteps.Add(step);
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
    {
        SimulationProject? project;
        do
        {
            var step = await RunOneStepAsync(simulationId, cancellationToken);
            if (step is null)
            {
                break;
            }

            project = await db.SimulationProjects.FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);
        }
        while (project is not null && project.CurrentStep < project.TotalSteps);

        await SaveMetricsAsync(simulationId, cancellationToken);

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

    private async Task<string> DeterminePhaseAsync(
        SimulationProject project,
        int stepNo,
        IReadOnlyCollection<AgentAction> currentStepActions,
        KnowledgeTimelinePoint knowledgePoint,
        CancellationToken cancellationToken)
    {
        if (stepNo <= 2)
        {
            return SimulationPhase.Forming;
        }

        var minStep = Math.Max(1, stepNo - (PhaseWindowSize - 1));
        var recentActions = await db.AgentActions
            .Where(action => action.SimulationProjectId == project.Id && action.StepNo >= minStep && action.StepNo <= stepNo)
            .ToListAsync(cancellationToken);
        recentActions.AddRange(currentStepActions);

        if (recentActions.Count == 0)
        {
            return SimulationPhase.Forming;
        }

        var counts = AgentActionType.All.ToDictionary(
            action => action,
            action => recentActions.Count(item => item.Action == action));

        var total = recentActions.Count;
        double Ratio(string action) => counts[action] / (double)total;
        var collaborationRatio = (counts[AgentActionType.ShareInfo] + counts[AgentActionType.AskHelp] + counts[AgentActionType.SupportOther]) / (double)total;
        var ideaRatio = Ratio(AgentActionType.ProposeIdea);
        var criticismRatio = Ratio(AgentActionType.Criticize);
        var soloRatio = Ratio(AgentActionType.WorkAlone);
        var waitRatio = Ratio(AgentActionType.Wait);
        var shareRatio = Ratio(AgentActionType.ShareInfo);
        var supportRatio = Ratio(AgentActionType.SupportOther);
        var diversity = counts.Values.Count(value => value > 0);
        var adaptationSignal = collaborationRatio + ideaRatio;
        var networkMetrics = NetworkMetricsCalculator.CalculateFromAgents(project.Agents, project.EffectiveTrustThreshold);

        if (waitRatio >= 0.35 && ideaRatio <= 0.15 && shareRatio <= 0.15)
        {
            return SimulationPhase.Collapse;
        }

        if ((knowledgePoint.ChallengeActive || knowledgePoint.ChallengeOccurred)
            && !knowledgePoint.ChallengeResolved
            && knowledgePoint.ExplorationScore >= 0.40
            && knowledgePoint.KnowledgeRecombinationScore >= 0.40
            && knowledgePoint.KnowledgeReconfigurationScore >= 0.40
            && adaptationSignal >= 0.55
            && soloRatio < 0.35
            && supportRatio >= 0.08)
        {
            return SimulationPhase.Adaptation;
        }

        if (criticismRatio >= 0.3 && supportRatio <= 0.1 && knowledgePoint.KnowledgeRewiringScore < 0.45)
        {
            return SimulationPhase.Chaos;
        }

        if (knowledgePoint.ChallengeActive
            && knowledgePoint.ChallengeGap > 0.15
            && soloRatio >= 0.30
            && supportRatio <= 0.10)
        {
            return SimulationPhase.Silo;
        }

        if (soloRatio >= 0.35 && shareRatio <= 0.15 && knowledgePoint.KnowledgeRewiringScore < 0.35)
        {
            return SimulationPhase.Silo;
        }

        if (((ideaRatio >= 0.20
                && shareRatio >= 0.14
                && supportRatio >= 0.10
                && networkMetrics.EffectiveNetworkDensity >= 0.35
                && knowledgePoint.SerendipityOccurred
                && knowledgePoint.KnowledgeRecombinationScore >= 0.40
                && knowledgePoint.KnowledgeRewiringScore >= 0.45))
            || (knowledgePoint.ChallengeResolved
                && knowledgePoint.ChallengeResolutionScore >= 0.45
                && knowledgePoint.SerendipityOccurred
                && knowledgePoint.KnowledgeRecombinationScore >= 0.40
                && knowledgePoint.KnowledgeReconfigurationScore >= 0.45
                && ideaRatio >= 0.18
                && shareRatio >= 0.12
                && supportRatio >= 0.10
                && networkMetrics.EffectiveNetworkDensity >= 0.35))
        {
            return SimulationPhase.Emergent;
        }

        if (collaborationRatio >= 0.45
            || (knowledgePoint.KnowledgeStock >= 0.35
                && (!knowledgePoint.SerendipityOccurred || knowledgePoint.KnowledgeRecombinationScore < 0.35)))
        {
            return SimulationPhase.Learning;
        }

        if (diversity >= 5
            && soloRatio < 0.3
            && criticismRatio < 0.25
            && waitRatio < 0.25
            && knowledgePoint.KnowledgeRewiringScore < 0.45
            && !knowledgePoint.ChallengeActive)
        {
            return SimulationPhase.Stable;
        }

        return SimulationPhase.Stable;
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
                challengeWindowStarted,
                effectiveChallengeLevel,
                project.PsychologicalSafetyLevel,
                actions.WorkAloneRate)
            : 0;
        var serendipityOccurred = project.EnableSerendipity
            && serendipityScore >= Math.Clamp(project.SerendipityThreshold, 0, 1);
        var knowledgeRecombinationScore = project.EnableSerendipity
            ? SerendipityAnalysisService.CalculateKnowledgeRecombinationScore(
                serendipityScore,
                actions,
                project.PsychologicalSafetyLevel,
                project.KnowledgeRecombinationRate,
                project.ConstructiveCriticismBonus)
            : 0;
        var knowledgeReconfigurationScore = KnowledgeAnalysisService.CalculateKnowledgeReconfigurationScore(
            rewiringScore,
            effectiveChallengeLevel,
            actions,
            project.PsychologicalSafetyLevel,
            challengeWindowStarted,
            knowledgeRecombinationScore,
            project.ConstructiveCriticismBonus);
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

    private sealed record TrustPairChange(double? Before, double? After);
    private sealed record TrustChangeMetrics(double? Before, double? Delta, double? After);
}
