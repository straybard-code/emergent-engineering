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
    private static readonly string[] ConstructiveCriticismKeywords = ["\u5fc3\u7406\u7684\u5b89\u5168\u6027", "\u5931\u6557\u3092\u8a31\u5bb9", "\u5efa\u8a2d\u7684\u6279\u5224"];
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
        List<AgentAction> currentStepActions = [];

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

            var trustMetrics = ApplyTrustUpdate(project, agent, agentAction);
            agentAction.TrustBefore = trustMetrics.Before;
            agentAction.TrustDelta = trustMetrics.Delta;
            agentAction.TrustAfter = trustMetrics.After;
            currentStepActions.Add(agentAction);
            db.AgentActions.Add(agentAction);
        }

        var stepActionSummary = ActionDistributionCalculator.Calculate(currentStepActions.Select(action => action.Action));
        var knowledgePoint = ApplyKnowledgeAndShock(project, stepNo, stepActionSummary);

        project.Phase = await DeterminePhaseAsync(
            project,
            stepNo,
            currentStepActions,
            knowledgePoint.KnowledgeRewiringScore,
            knowledgePoint.KnowledgeStock,
            cancellationToken);
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
            knowledgeStock = knowledgePoint.KnowledgeStock,
            knowledgeDiversity = knowledgePoint.KnowledgeDiversity,
            externalShockLevel = knowledgePoint.ExternalShockLevel,
            crossDomainExposure = knowledgePoint.CrossDomainExposure,
            knowledgeRewiringScore = knowledgePoint.KnowledgeRewiringScore,
            shockOccurred = knowledgePoint.ShockOccurred,
            shockType = knowledgePoint.ShockType,
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
        double knowledgeRewiringScore,
        double knowledgeStock,
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

        if (waitRatio >= 0.35 && ideaRatio <= 0.15 && shareRatio <= 0.15)
        {
            return SimulationPhase.Collapse;
        }

        if (criticismRatio >= 0.3 && supportRatio <= 0.1 && knowledgeRewiringScore < 0.45)
        {
            return SimulationPhase.Chaos;
        }

        if (soloRatio >= 0.35 && shareRatio <= 0.15 && knowledgeRewiringScore < 0.35)
        {
            return SimulationPhase.Silo;
        }

        if (ideaRatio >= 0.20
            && shareRatio >= 0.14
            && supportRatio >= 0.10
            && knowledgeRewiringScore >= 0.45)
        {
            return SimulationPhase.Emergent;
        }

        if (collaborationRatio >= 0.45 || knowledgeStock >= 0.35)
        {
            return SimulationPhase.Learning;
        }

        if (diversity >= 5
            && soloRatio < 0.3
            && criticismRatio < 0.25
            && waitRatio < 0.25
            && knowledgeRewiringScore < 0.45)
        {
            return SimulationPhase.Stable;
        }

        return SimulationPhase.Stable;
    }

    private TrustChangeMetrics ApplyTrustUpdate(SimulationProject project, Agent actor, AgentAction agentAction)
    {
        var targetName = NormalizeTarget(agentAction.TargetAgentName);
        var targetAgent = project.Agents.FirstOrDefault(candidate =>
            candidate.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        var constructiveCriticism = ContainsAny(project.BoundaryConditions, ConstructiveCriticismKeywords);
        List<TrustPairChange> actorSideChanges = [];

        switch (agentAction.Action)
        {
            case AgentActionType.SupportOther:
                if (targetAgent is not null)
                {
                    actorSideChanges.Add(UpdateTrust(actor, targetAgent, 0.10));
                    UpdateTrust(targetAgent, actor, 0.15);
                }
                break;

            case AgentActionType.AskHelp:
                if (targetAgent is not null)
                {
                    actorSideChanges.Add(UpdateTrust(actor, targetAgent, 0.05));
                    UpdateTrust(targetAgent, actor, 0.05);
                }
                break;

            case AgentActionType.ShareInfo:
                foreach (var otherAgent in project.Agents.Where(candidate => candidate.Id != actor.Id))
                {
                    actorSideChanges.Add(UpdateTrust(actor, otherAgent, 0.03));
                    UpdateTrust(otherAgent, actor, 0.03);
                }
                break;

            case AgentActionType.ProposeIdea:
                foreach (var otherAgent in project.Agents.Where(candidate => candidate.Id != actor.Id))
                {
                    actorSideChanges.Add(UpdateTrust(actor, otherAgent, 0.02));
                }
                break;

            case AgentActionType.Criticize:
                if (targetAgent is not null)
                {
                    if (constructiveCriticism)
                    {
                        actorSideChanges.Add(UpdateTrust(actor, targetAgent, 0.02));
                        UpdateTrust(targetAgent, actor, 0.02);
                    }
                    else
                    {
                        actorSideChanges.Add(UpdateTrust(actor, targetAgent, -0.05));
                        UpdateTrust(targetAgent, actor, -0.10);
                    }
                }
                break;
        }

        return CalculateTrustMetrics(actorSideChanges);
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

    private static bool ContainsAny(string source, IReadOnlyCollection<string> keywords)
    {
        return keywords.Any(keyword => source.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static TrustPairChange UpdateTrust(Agent sourceAgent, Agent targetAgent, double delta)
    {
        if (sourceAgent.Id == targetAgent.Id)
        {
            return new TrustPairChange(null, null);
        }

        var trustMap = TrustJsonUtility.Deserialize(sourceAgent.TrustJson);
        var before = trustMap.TryGetValue(targetAgent.Name, out var currentValue) ? currentValue : 0;
        var after = TrustJsonUtility.Clamp(before + delta);
        trustMap[targetAgent.Name] = after;
        sourceAgent.TrustJson = JsonSerializer.Serialize(trustMap, JsonOptions);
        return new TrustPairChange(before, after);
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

    private static KnowledgeTimelinePoint ApplyKnowledgeAndShock(
        SimulationProject project,
        int stepNo,
        ActionDistributionSummary actions)
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

        var knowledgeStockDelta = KnowledgeAnalysisService.CalculateKnowledgeStockDelta(actions);
        var knowledgeDiversityDelta = KnowledgeAnalysisService.CalculateKnowledgeDiversityDelta(
            actions,
            project.PsychologicalSafetyLevel,
            project.CrossDomainExposure,
            effectiveExternalShockLevel,
            project.RewiringSensitivity);

        project.KnowledgeStock = Math.Clamp(project.KnowledgeStock + knowledgeStockDelta, 0, 1);
        project.KnowledgeDiversity = Math.Clamp(project.KnowledgeDiversity + knowledgeDiversityDelta, 0, 1);

        var rewiringScore = KnowledgeAnalysisService.CalculateKnowledgeRewiringScore(
            project.KnowledgeDiversity,
            project.CrossDomainExposure,
            effectiveExternalShockLevel,
            actions,
            project.PsychologicalSafetyLevel,
            project.RewiringSensitivity);

        var interpretation = KnowledgeAnalysisService.BuildInterpretation(
            project.KnowledgeStock,
            project.KnowledgeDiversity,
            effectiveExternalShockLevel,
            project.CrossDomainExposure,
            rewiringScore,
            actions.WorkAloneRate);

        return new KnowledgeTimelinePoint
        {
            StepNo = stepNo,
            KnowledgeStock = Math.Round(project.KnowledgeStock, 4),
            KnowledgeDiversity = Math.Round(project.KnowledgeDiversity, 4),
            ExternalShockLevel = Math.Round(effectiveExternalShockLevel, 4),
            CrossDomainExposure = Math.Round(project.CrossDomainExposure, 4),
            KnowledgeRewiringScore = rewiringScore,
            ShockOccurred = shockOccurred,
            ShockType = shockOccurred ? project.ShockType : ShockTypes.None,
            Interpretation = interpretation
        };
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

    private sealed record TrustPairChange(double? Before, double? After);
    private sealed record TrustChangeMetrics(double? Before, double? Delta, double? After);
}
