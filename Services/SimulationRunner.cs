using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Services;

public sealed class SimulationRunner(AppDbContext db, ILlmService llmService) : ISimulationRunner
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
            var llm = await llmService.CompleteAgentTurnAsync(prompt, cancellationToken);
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
                Message = llm.Message.Trim(),
                Memory = agent.Memory,
                Action = llm.Action,
                TargetAgentName = llm.TargetAgentName.Trim(),
                RawLlmResponse = string.IsNullOrWhiteSpace(llm.RawResponse) ? JsonSerializer.Serialize(llm, JsonOptions) : llm.RawResponse,
                CreatedAt = DateTime.UtcNow
            };

            ApplyTrustUpdate(project, agent, agentAction);
            currentStepActions.Add(agentAction);
            db.AgentActions.Add(agentAction);
        }

        project.Phase = await DeterminePhaseAsync(project, stepNo, currentStepActions, cancellationToken);
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
            Agents = project.Agents.Select(agent => new
            {
                agent.Id,
                agent.Name,
                agent.Role,
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

        db.SimulationSteps.Add(step);
        await db.SaveChangesAsync(cancellationToken);
        return step;
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

        return await db.SimulationProjects.FirstOrDefaultAsync(item => item.Id == simulationId, cancellationToken);
    }

    private static string BuildPrompt(SimulationProject project, Agent agent, IReadOnlyCollection<AgentAction> recentActions)
    {
        var recent = recentActions.Count == 0
            ? "No previous messages."
            : string.Join("\n", recentActions.Select(action => $"- {action.Agent?.Name ?? "Unknown"}: {action.Message} (action: {action.Action})"));
        var roster = string.Join(", ", project.Agents.Where(item => item.Id != agent.Id).OrderBy(item => item.Id).Select(item => item.Name));

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

        if (criticismRatio >= 0.3 && supportRatio <= 0.1)
        {
            return SimulationPhase.Chaos;
        }

        if (soloRatio >= 0.35 && shareRatio <= 0.15)
        {
            return SimulationPhase.Silo;
        }

        if (ideaRatio >= 0.22 && shareRatio >= 0.14 && criticismRatio >= 0.08 && supportRatio >= 0.08)
        {
            return SimulationPhase.Emergent;
        }

        if (collaborationRatio >= 0.45)
        {
            return SimulationPhase.Learning;
        }

        if (diversity >= 5 && soloRatio < 0.3 && criticismRatio < 0.25 && waitRatio < 0.25)
        {
            return SimulationPhase.Stable;
        }

        return SimulationPhase.Stable;
    }

    private void ApplyTrustUpdate(SimulationProject project, Agent actor, AgentAction agentAction)
    {
        var targetName = NormalizeTarget(agentAction.TargetAgentName);
        var targetAgent = project.Agents.FirstOrDefault(candidate =>
            candidate.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
        var constructiveCriticism = ContainsAny(project.BoundaryConditions, ConstructiveCriticismKeywords);

        switch (agentAction.Action)
        {
            case AgentActionType.SupportOther:
                if (targetAgent is not null)
                {
                    UpdateTrust(actor, targetAgent, 0.10);
                    UpdateTrust(targetAgent, actor, 0.15);
                }
                break;

            case AgentActionType.AskHelp:
                if (targetAgent is not null)
                {
                    UpdateTrust(actor, targetAgent, 0.05);
                    UpdateTrust(targetAgent, actor, 0.05);
                }
                break;

            case AgentActionType.ShareInfo:
                foreach (var otherAgent in project.Agents.Where(candidate => candidate.Id != actor.Id))
                {
                    UpdateTrust(actor, otherAgent, 0.03);
                    UpdateTrust(otherAgent, actor, 0.03);
                }
                break;

            case AgentActionType.ProposeIdea:
                foreach (var otherAgent in project.Agents.Where(candidate => candidate.Id != actor.Id))
                {
                    UpdateTrust(actor, otherAgent, 0.02);
                }
                break;

            case AgentActionType.Criticize:
                if (targetAgent is not null)
                {
                    if (constructiveCriticism)
                    {
                        UpdateTrust(actor, targetAgent, 0.02);
                        UpdateTrust(targetAgent, actor, 0.02);
                    }
                    else
                    {
                        UpdateTrust(actor, targetAgent, -0.05);
                        UpdateTrust(targetAgent, actor, -0.10);
                    }
                }
                break;
        }
    }

    private static string NormalizeTarget(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Trim() == "-" ? "" : value.Trim();
    }

    private static bool ContainsAny(string source, IReadOnlyCollection<string> keywords)
    {
        return keywords.Any(keyword => source.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static void UpdateTrust(Agent sourceAgent, Agent targetAgent, double delta)
    {
        if (sourceAgent.Id == targetAgent.Id)
        {
            return;
        }

        var trustMap = DeserializeTrust(sourceAgent.TrustJson);
        trustMap[targetAgent.Name] = ClampTrust(trustMap.GetValueOrDefault(targetAgent.Name) + delta);
        sourceAgent.TrustJson = JsonSerializer.Serialize(trustMap, JsonOptions);
    }

    private static Dictionary<string, double> DeserializeTrust(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            }

            var trustMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (TryReadTrustValue(property.Value, out var value))
                {
                    trustMap[property.Name] = ClampTrust(value);
                }
            }

            return trustMap;
        }
        catch
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static bool TryReadTrustValue(JsonElement element, out double value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.TryGetDouble(out value);

            case JsonValueKind.String:
                return double.TryParse(element.GetString(), out value);

            default:
                value = 0;
                return false;
        }
    }

    private static double ClampTrust(double value)
    {
        return Math.Clamp(Math.Round(value, 2), -1.0, 1.0);
    }
}
