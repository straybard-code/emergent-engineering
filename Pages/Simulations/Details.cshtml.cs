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
    public EmergenceFingerprint? Fingerprint { get; private set; }
    public string MostCommonPhaseTransitionPattern { get; private set; } = "-";
    public int? SelectedAgentId { get; private set; }
    public bool IsCompleted => Project is not null && Project.CurrentStep >= Project.TotalSteps;
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
        var edgeRowsByStep = NetworkMetricsCalculator.BuildEdgeRowsByStep(Project.Agents, trustSnapshots, Project.CurrentStep);

        NetworkSnapshots = BuildNetworkSnapshots(
            Project,
            trustSnapshots,
            phaseByStep,
            NetworkMetricsCalculator.NormalizeThreshold(Project.EffectiveTrustThreshold));
        FinalNetworkMetrics = CalculateNetworkMetrics(Project.Agents, finalTrustRows, Project.EffectiveTrustThreshold);
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

    private static int MapPhase(string phase)
    {
        return phase switch
        {
            SimulationPhase.Forming => 0,
            SimulationPhase.Learning => 1,
            SimulationPhase.Adaptation => 2,
            SimulationPhase.Stable => 3,
            SimulationPhase.Emergent => 4,
            SimulationPhase.Silo => -1,
            SimulationPhase.Chaos => -2,
            SimulationPhase.Collapse => -3,
            _ => 0
        };
    }

    private static List<NetworkSnapshot> BuildNetworkSnapshots(
        SimulationProject project,
        IReadOnlyCollection<TrustSnapshot> trustSnapshots,
        IReadOnlyDictionary<int, string> phaseByStep,
        double effectiveTrustThreshold)
    {
        if (project.Agents.Count == 0 || project.CurrentStep <= 0)
        {
            return [];
        }

        if (trustSnapshots.Count == 0)
        {
            return BuildFallbackNetworkSnapshots(project, phaseByStep, effectiveTrustThreshold);
        }

        return trustSnapshots
            .GroupBy(snapshot => snapshot.StepNo)
            .OrderBy(group => group.Key)
            .Select(group => CreateSnapshotFromRows(
                group.Key,
                phaseByStep.GetValueOrDefault(group.Key, group.Key <= 2 ? SimulationPhase.Forming : project.Phase),
                project.Agents,
                effectiveTrustThreshold,
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
                KnowledgeReconfigurationScore = knowledge?.KnowledgeReconfigurationScore ?? 0
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
}
