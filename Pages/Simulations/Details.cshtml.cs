using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Simulations;

public sealed class DetailsModel(AppDbContext db, ISimulationRunner runner) : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const double TrustDisplayThreshold = 0.01;
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
    public int? SelectedAgentId { get; private set; }
    public bool IsCompleted => Project is not null && Project.CurrentStep >= Project.TotalSteps;

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

        PhaseHistory = await db.SimulationSteps
            .Where(step => step.SimulationProjectId == id)
            .OrderBy(step => step.StepNo)
            .Select(step => new PhasePoint
            {
                StepNo = step.StepNo,
                Phase = step.Phase,
                PhaseValue = MapPhase(step.Phase)
            })
            .ToListAsync();

        var phaseByStep = PhaseHistory.ToDictionary(point => point.StepNo, point => point.Phase);
        var trustSnapshots = await db.TrustSnapshots
            .Where(snapshot => snapshot.SimulationProjectId == id)
            .OrderBy(snapshot => snapshot.StepNo)
            .ThenBy(snapshot => snapshot.SourceAgentId)
            .ThenBy(snapshot => snapshot.TargetAgentId)
            .ToListAsync();

        NetworkSnapshots = BuildNetworkSnapshots(Project, trustSnapshots, phaseByStep);

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

    private static int MapPhase(string phase)
    {
        return phase switch
        {
            SimulationPhase.Forming => 0,
            SimulationPhase.Learning => 1,
            SimulationPhase.Stable => 2,
            SimulationPhase.Emergent => 3,
            SimulationPhase.Silo => -1,
            SimulationPhase.Chaos => -2,
            SimulationPhase.Collapse => -3,
            _ => 0
        };
    }

    private static List<NetworkSnapshot> BuildNetworkSnapshots(
        SimulationProject project,
        IReadOnlyCollection<TrustSnapshot> trustSnapshots,
        IReadOnlyDictionary<int, string> phaseByStep)
    {
        if (project.Agents.Count == 0 || project.CurrentStep <= 0)
        {
            return [];
        }

        if (trustSnapshots.Count == 0)
        {
            return BuildFallbackNetworkSnapshots(project, phaseByStep);
        }

        return trustSnapshots
            .GroupBy(snapshot => snapshot.StepNo)
            .OrderBy(group => group.Key)
            .Select(group => CreateSnapshotFromRows(
                group.Key,
                phaseByStep.GetValueOrDefault(group.Key, group.Key <= 2 ? SimulationPhase.Forming : project.Phase),
                project.Agents,
                group.Select(snapshot => new TrustRow(
                    snapshot.SourceAgentId,
                    snapshot.TargetAgentId,
                    snapshot.SourceAgentName,
                    snapshot.TargetAgentName,
                    TrustJsonUtility.Clamp(snapshot.TrustValue)))
                    .ToList()))
            .ToList();
    }

    private static List<NetworkSnapshot> BuildFallbackNetworkSnapshots(
        SimulationProject project,
        IReadOnlyDictionary<int, string> phaseByStep)
    {
        var rows = CreateTrustRowsFromCurrentState(project.Agents);
        List<NetworkSnapshot> snapshots = [];

        for (var stepNo = 1; stepNo <= project.CurrentStep; stepNo++)
        {
            snapshots.Add(CreateSnapshotFromRows(
                stepNo,
                phaseByStep.GetValueOrDefault(stepNo, stepNo <= 2 ? SimulationPhase.Forming : project.Phase),
                project.Agents,
                rows));
        }

        return snapshots;
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

    private static NetworkSnapshot CreateSnapshotFromRows(
        int stepNo,
        string phase,
        IReadOnlyCollection<Agent> agents,
        IReadOnlyCollection<TrustRow> rows)
    {
        var agentList = agents.OrderBy(agent => agent.Id).ToList();
        var activityScores = agentList.ToDictionary(agent => agent.Id, _ => 0.0);
        var connectedAgents = agentList.ToDictionary(agent => agent.Id, _ => false);
        List<double> trustValues = [];
        List<double> absTrustValues = [];
        List<NetworkEdge> edges = [];

        foreach (var row in rows)
        {
            var trust = TrustJsonUtility.Clamp(row.TrustValue);
            trustValues.Add(trust);
            absTrustValues.Add(Math.Abs(trust));

            if (Math.Abs(trust) <= TrustDisplayThreshold)
            {
                continue;
            }

            activityScores[row.SourceAgentId] += Math.Abs(trust);
            activityScores[row.TargetAgentId] += Math.Abs(trust);
            connectedAgents[row.SourceAgentId] = true;
            connectedAgents[row.TargetAgentId] = true;
            edges.Add(new NetworkEdge
            {
                SourceAgentId = row.SourceAgentId,
                TargetAgentId = row.TargetAgentId,
                SourceAgentName = row.SourceAgentName,
                TargetAgentName = row.TargetAgentName,
                Trust = Math.Round(trust, 2),
                StrokeWidth = Math.Round(1 + (Math.Clamp(Math.Abs(trust), 0, 1) * 6), 2),
                StrokeColor = trust < 0 ? "#d06a6a" : "#8bb8ef"
            });
        }

        var hubAgentId = activityScores
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Key)
            .FirstOrDefault().Key;
        var hubAgentName = activityScores.GetValueOrDefault(hubAgentId) > 0
            ? agentList.FirstOrDefault(agent => agent.Id == hubAgentId)?.Name ?? "-"
            : "-";

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
                    IsHub = hubAgentId == agent.Id && hubAgentName != "-"
                };
            })
            .ToList();

        return new NetworkSnapshot
        {
            StepNo = stepNo,
            Phase = phase,
            AverageTrust = trustValues.Count == 0 ? 0 : Math.Round(trustValues.Average(), 2),
            AverageAbsTrust = absTrustValues.Count == 0 ? 0 : Math.Round(absTrustValues.Average(), 2),
            HubAgentName = hubAgentName,
            IsolatedCount = connectedAgents.Count(pair => !pair.Value),
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
}
