using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class NetworkMetricsCalculator
{
    public const double NoLinkThreshold = 0.01;
    public static readonly double[] StandardThresholdSweepValues = [0.10, 0.20, 0.30, 0.40, 0.50, 0.60, 0.70, 0.80, 0.90];

    public static double NormalizeThreshold(double threshold)
    {
        return Math.Clamp(threshold, 0, 1);
    }

    public static TrustLinkCategory Classify(double trustValue, double effectiveTrustThreshold)
    {
        var trust = TrustJsonUtility.Clamp(trustValue);
        var threshold = NormalizeThreshold(effectiveTrustThreshold);

        if (trust < -NoLinkThreshold)
        {
            return TrustLinkCategory.Negative;
        }

        if (trust <= NoLinkThreshold)
        {
            return TrustLinkCategory.None;
        }

        return trust >= threshold
            ? TrustLinkCategory.Strong
            : TrustLinkCategory.Weak;
    }

    public static List<NetworkTrustEdgeRef> CreateRowsFromAgents(IReadOnlyCollection<Agent> agents)
    {
        var trustMaps = agents.ToDictionary(agent => agent.Id, agent => TrustJsonUtility.Deserialize(agent.TrustJson));
        List<NetworkTrustEdgeRef> rows = [];

        foreach (var sourceAgent in agents)
        {
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                var trustMap = trustMaps[sourceAgent.Id];
                var trustValue = trustMap.TryGetValue(targetAgent.Name, out var value) ? value : 0;
                rows.Add(new NetworkTrustEdgeRef(
                    sourceAgent.Id,
                    targetAgent.Id,
                    sourceAgent.Name,
                    targetAgent.Name,
                    TrustJsonUtility.Clamp(trustValue)));
            }
        }

        return rows;
    }

    public static NetworkMetricsResult CalculateFromAgents(IReadOnlyCollection<Agent> agents, double effectiveTrustThreshold)
    {
        var refs = agents
            .OrderBy(agent => agent.Id)
            .Select(agent => new NetworkAgentRef(agent.Id, agent.Name))
            .ToList();

        return Calculate(refs, CreateRowsFromAgents(agents), effectiveTrustThreshold);
    }

    public static Dictionary<int, NetworkMetricsResult> BuildMetricsByStep(
        IReadOnlyCollection<Agent> agents,
        IReadOnlyCollection<TrustSnapshot> snapshots,
        double effectiveTrustThreshold,
        int currentStep)
    {
        if (agents.Count == 0 || currentStep <= 0)
        {
            return new Dictionary<int, NetworkMetricsResult>();
        }

        if (snapshots.Count == 0)
        {
            var fallback = CalculateFromAgents(agents, effectiveTrustThreshold);
            return Enumerable.Range(1, currentStep)
                .ToDictionary(stepNo => stepNo, _ => fallback);
        }

        var agentRefs = agents
            .OrderBy(agent => agent.Id)
            .Select(agent => new NetworkAgentRef(agent.Id, agent.Name))
            .ToList();

        return snapshots
            .GroupBy(snapshot => snapshot.StepNo)
            .ToDictionary(
                group => group.Key,
                group => Calculate(
                    agentRefs,
                    group.Select(snapshot => new NetworkTrustEdgeRef(
                        snapshot.SourceAgentId,
                        snapshot.TargetAgentId,
                        snapshot.SourceAgentName,
                        snapshot.TargetAgentName,
                        TrustJsonUtility.Clamp(snapshot.TrustValue)))
                        .ToList(),
                    effectiveTrustThreshold));
    }

    public static Dictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>> BuildEdgeRowsByStep(
        IReadOnlyCollection<Agent> agents,
        IReadOnlyCollection<TrustSnapshot> snapshots,
        int currentStep)
    {
        if (agents.Count == 0 || currentStep <= 0)
        {
            return new Dictionary<int, IReadOnlyCollection<NetworkTrustEdgeRef>>();
        }

        if (snapshots.Count == 0)
        {
            var fallback = CreateRowsFromAgents(agents);
            return Enumerable.Range(1, currentStep)
                .ToDictionary(stepNo => stepNo, _ => (IReadOnlyCollection<NetworkTrustEdgeRef>)fallback);
        }

        return snapshots
            .GroupBy(snapshot => snapshot.StepNo)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<NetworkTrustEdgeRef>)group.Select(snapshot => new NetworkTrustEdgeRef(
                        snapshot.SourceAgentId,
                        snapshot.TargetAgentId,
                        snapshot.SourceAgentName,
                        snapshot.TargetAgentName,
                        TrustJsonUtility.Clamp(snapshot.TrustValue)))
                    .ToList());
    }

    public static NetworkMetricsResult Calculate(
        IReadOnlyCollection<NetworkAgentRef> agents,
        IReadOnlyCollection<NetworkTrustEdgeRef> edges,
        double effectiveTrustThreshold)
    {
        var threshold = NormalizeThreshold(effectiveTrustThreshold);
        var agentList = agents.OrderBy(agent => agent.AgentId).ToList();
        if (agentList.Count == 0)
        {
            return new NetworkMetricsResult();
        }

        var maxEdges = agentList.Count * (agentList.Count - 1);
        var adjacency = agentList.ToDictionary(agent => agent.AgentId, _ => new HashSet<int>());
        var strongScores = agentList.ToDictionary(agent => agent.AgentId, _ => 0.0);
        List<double> trustValues = [];
        List<double> absTrustValues = [];

        var networkLinkCount = 0;
        var strongLinkCount = 0;
        var weakLinkCount = 0;

        foreach (var edge in edges)
        {
            var trust = TrustJsonUtility.Clamp(edge.TrustValue);
            trustValues.Add(trust);
            absTrustValues.Add(Math.Abs(trust));

            var category = Classify(trust, threshold);
            switch (category)
            {
                case TrustLinkCategory.Strong:
                    networkLinkCount++;
                    strongLinkCount++;
                    adjacency[edge.SourceAgentId].Add(edge.TargetAgentId);
                    adjacency[edge.TargetAgentId].Add(edge.SourceAgentId);
                    strongScores[edge.SourceAgentId] += Math.Abs(trust);
                    strongScores[edge.TargetAgentId] += Math.Abs(trust);
                    break;

                case TrustLinkCategory.Weak:
                    networkLinkCount++;
                    weakLinkCount++;
                    break;
            }
        }

        var componentCount = CountComponents(agentList.Select(agent => agent.AgentId).ToList(), adjacency);
        var isolatedAgentIds = adjacency
            .Where(pair => pair.Value.Count == 0)
            .Select(pair => pair.Key)
            .ToHashSet();

        var hub = strongScores
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .FirstOrDefault();

        var hasStrongLinks = strongLinkCount > 0 && hub.Value > 0;

        return new NetworkMetricsResult
        {
            AverageTrust = trustValues.Count == 0 ? 0 : Math.Round(trustValues.Average(), 2),
            AverageAbsTrust = absTrustValues.Count == 0 ? 0 : Math.Round(absTrustValues.Average(), 2),
            NetworkDensity = maxEdges <= 0 ? 0 : Math.Round(networkLinkCount / (double)maxEdges, 3),
            EffectiveNetworkDensity = maxEdges <= 0 ? 0 : Math.Round(strongLinkCount / (double)maxEdges, 3),
            StrongLinkCount = strongLinkCount,
            WeakLinkCount = weakLinkCount,
            ComponentCount = componentCount,
            IsolatedCount = isolatedAgentIds.Count,
            HubAgentId = hasStrongLinks ? hub.Key : null,
            HubAgentName = hasStrongLinks
                ? agentList.FirstOrDefault(agent => agent.AgentId == hub.Key)?.AgentName ?? "-"
                : "-",
            HubScore = hasStrongLinks ? Math.Round(hub.Value, 2) : 0,
            IsolatedAgentIds = isolatedAgentIds
        };
    }

    private static int CountComponents(
        IReadOnlyCollection<int> agentIds,
        IReadOnlyDictionary<int, HashSet<int>> adjacency)
    {
        if (agentIds.Count == 0)
        {
            return 0;
        }

        var visited = new HashSet<int>();
        var count = 0;

        foreach (var agentId in agentIds)
        {
            if (!visited.Add(agentId))
            {
                continue;
            }

            count++;
            var stack = new Stack<int>();
            stack.Push(agentId);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!adjacency.TryGetValue(current, out var neighbors))
                {
                    continue;
                }

                foreach (var neighbor in neighbors)
                {
                    if (visited.Add(neighbor))
                    {
                        stack.Push(neighbor);
                    }
                }
            }
        }

        return count;
    }
}

public sealed record NetworkAgentRef(int AgentId, string AgentName);

public sealed record NetworkTrustEdgeRef(
    int SourceAgentId,
    int TargetAgentId,
    string SourceAgentName,
    string TargetAgentName,
    double TrustValue);

public sealed class NetworkMetricsResult
{
    public double AverageTrust { get; init; }
    public double AverageAbsTrust { get; init; }
    public double NetworkDensity { get; init; }
    public double EffectiveNetworkDensity { get; init; }
    public int StrongLinkCount { get; init; }
    public int WeakLinkCount { get; init; }
    public int ComponentCount { get; init; }
    public int IsolatedCount { get; init; }
    public int? HubAgentId { get; init; }
    public string HubAgentName { get; init; } = "-";
    public double HubScore { get; init; }
    public IReadOnlySet<int> IsolatedAgentIds { get; init; } = new HashSet<int>();
}

public enum TrustLinkCategory
{
    None,
    Weak,
    Strong,
    Negative
}
