using System.Text.Json;
using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class ThanksCoinService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static ThanksCoinStepResult Apply(
        SimulationProject project,
        ActionDistributionSummary actions,
        KnowledgeTimelinePoint? previousKnowledgePoint,
        int stepNo)
    {
        var agents = project.Agents.OrderBy(agent => agent.Id).ToList();
        if (agents.Count == 0)
        {
            return BuildEmptyResult(project, previousKnowledgePoint, agents);
        }

        var respectMatrix = LoadRespectMatrix(previousKnowledgePoint?.RespectJson, project, agents);
        var baselineMetrics = CalculateRespectMetrics(project, agents, respectMatrix);
        var trustMaps = agents.ToDictionary(
            agent => agent.Id,
            agent => TrustJsonUtility.Deserialize(agent.TrustJson));
        var inboundTrustAverages = BuildInboundTrustAverages(agents, trustMaps);

        if (!project.EnableThanksCoin || project.ThanksCoinRate <= 0)
        {
            var noEventMetrics = CalculateRespectMetrics(project, agents, respectMatrix);
            return new ThanksCoinStepResult
            {
                Occurred = false,
                Count = 0,
                HelpCount = 0,
                IdeaCount = 0,
                ChallengeCount = 0,
                BridgeCount = 0,
                RespectJson = SerializeRespectMatrix(respectMatrix),
                AverageRespect = noEventMetrics.AverageRespect,
                RespectDensity = noEventMetrics.RespectDensity,
                RespectStrongLinks = noEventMetrics.RespectStrongLinks,
                RespectWeakLinks = noEventMetrics.RespectWeakLinks,
                RespectConcentration = noEventMetrics.RespectConcentration
            };
        }

        var totalEvents = Math.Max(
            1,
            (int)Math.Round(Math.Clamp(project.ThanksCoinRate, 0, 1) * agents.Count * (0.5 + baselineMetrics.AverageRespect)));
        var typeCounts = AllocateTypeCounts(project, actions, totalEvents);
        var eventTypes = ExpandEventTypes(typeCounts);
        var receiverCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var respectDeltaTotal = 0.0;
        var trustDeltaTotal = 0.0;
        var reconfigurationDeltaTotal = 0.0;
        var serendipityDeltaTotal = 0.0;
        var bridgeDeltaTotal = 0.0;
        var crossDomainExposureDeltaTotal = 0.0;
        var rewiringDeltaTotal = 0.0;
        var psychologicalSafetyDeltaTotal = 0.0;

        var helpCount = 0;
        var ideaCount = 0;
        var challengeCount = 0;
        var bridgeCount = 0;

        for (var index = 0; index < eventTypes.Count; index++)
        {
            var thanksType = eventTypes[index];
            var sender = agents[(stepNo + index) % agents.Count];
            var senderTrustMap = trustMaps[sender.Id];
            var receiver = SelectRecipient(
                thanksType,
                sender,
                agents,
                senderTrustMap,
                respectMatrix,
                inboundTrustAverages,
                project);

            if (receiver is null)
            {
                continue;
            }

            receiverCounts.TryGetValue(receiver.Name, out var receiverCount);
            receiverCounts[receiver.Name] = receiverCount + 1;

            var respectGain = Math.Clamp(project.ThanksCoinRespectGain, 0, 1);
            var trustGain = Math.Clamp(project.ThanksCoinTrustGain, 0, 1);
            var reconfigurationGain = Math.Clamp(project.ThanksCoinReconfigurationGain, 0, 1);
            var psychologicalSafetyGain = Math.Clamp(project.ThanksCoinPsychologicalSafetyGain, 0, 1);
            var bridgeGain = Math.Clamp(project.ThanksCoinBridgeGain, 0, 1);
            var challengeBonus = Math.Clamp(project.ThanksCoinChallengeBonus, 0, 1);

            switch (thanksType)
            {
                case ThanksCoinTypes.Help:
                    helpCount++;
                    trustDeltaTotal += ApplyTrustDelta(senderTrustMap, receiver.Name, trustGain);
                    trustDeltaTotal += ApplyTrustDelta(trustMaps[receiver.Id], sender.Name, trustGain * 0.5);
                    respectDeltaTotal += ApplyRespectDelta(respectMatrix, sender.Name, receiver.Name, respectGain);
                    respectDeltaTotal += ApplyRespectDelta(respectMatrix, receiver.Name, sender.Name, respectGain * 0.5);
                    break;

                case ThanksCoinTypes.Idea:
                    ideaCount++;
                    respectDeltaTotal += ApplyRespectDelta(respectMatrix, sender.Name, receiver.Name, respectGain * 1.10);
                    respectDeltaTotal += ApplyRespectDelta(respectMatrix, receiver.Name, sender.Name, respectGain * 1.10);
                    reconfigurationDeltaTotal += reconfigurationGain * 1.25;
                    break;

                case ThanksCoinTypes.Challenge:
                    challengeCount++;
                    respectDeltaTotal += ApplyRespectDelta(respectMatrix, sender.Name, receiver.Name, respectGain * 1.15);
                    respectDeltaTotal += ApplyRespectDelta(respectMatrix, receiver.Name, sender.Name, respectGain * 1.15);
                    psychologicalSafetyDeltaTotal += psychologicalSafetyGain + (challengeBonus * 0.45);
                    reconfigurationDeltaTotal += reconfigurationGain * 1.40;
                    trustDeltaTotal += ApplyTrustDelta(
                        senderTrustMap,
                        receiver.Name,
                        project.PsychologicalSafetyLevel >= 0.7 ? trustGain * 0.5 : trustGain * 0.2);
                    break;

                case ThanksCoinTypes.Bridge:
                    bridgeCount++;
                    respectDeltaTotal += ApplyRespectDelta(respectMatrix, sender.Name, receiver.Name, respectGain * 1.20);
                    respectDeltaTotal += ApplyRespectDelta(respectMatrix, receiver.Name, sender.Name, respectGain * 1.20);
                    serendipityDeltaTotal += bridgeGain * 1.25;
                    bridgeDeltaTotal += bridgeGain;
                    crossDomainExposureDeltaTotal += bridgeGain * 0.85;
                    rewiringDeltaTotal += bridgeGain * 0.85;
                    reconfigurationDeltaTotal += reconfigurationGain * 0.55;
                    trustDeltaTotal += ApplyTrustDelta(senderTrustMap, receiver.Name, trustGain * 0.25);
                    trustDeltaTotal += ApplyTrustDelta(trustMaps[receiver.Id], sender.Name, trustGain * 0.15);
                    break;
            }
        }

        var totalCount = helpCount + ideaCount + challengeCount + bridgeCount;
        var concentration = CalculateConcentration(receiverCounts, totalCount);

        if (concentration > 0.60)
        {
            reconfigurationDeltaTotal -= 0.05;
            serendipityDeltaTotal -= 0.05;
        }

        foreach (var agent in agents)
        {
            agent.TrustJson = JsonSerializer.Serialize(trustMaps[agent.Id], JsonOptions);
        }

        var metrics = CalculateRespectMetrics(project, agents, respectMatrix);
        var respectJson = SerializeRespectMatrix(respectMatrix);
        return new ThanksCoinStepResult
        {
            Occurred = totalCount > 0,
            Count = totalCount,
            HelpCount = helpCount,
            IdeaCount = ideaCount,
            ChallengeCount = challengeCount,
            BridgeCount = bridgeCount,
            RespectDelta = Math.Round(respectDeltaTotal, 4),
            TrustDelta = Math.Round(trustDeltaTotal, 4),
            ReconfigurationDelta = Math.Round(reconfigurationDeltaTotal, 4),
            SerendipityDelta = Math.Round(serendipityDeltaTotal, 4),
            BridgeDelta = Math.Round(bridgeDeltaTotal, 4),
            CrossDomainExposureDelta = Math.Round(crossDomainExposureDeltaTotal, 4),
            RewiringDelta = Math.Round(rewiringDeltaTotal, 4),
            PsychologicalSafetyDelta = Math.Round(psychologicalSafetyDeltaTotal, 4),
            ThanksConcentration = concentration,
            AverageRespect = metrics.AverageRespect,
            RespectDensity = metrics.RespectDensity,
            RespectStrongLinks = metrics.RespectStrongLinks,
            RespectWeakLinks = metrics.RespectWeakLinks,
            RespectConcentration = metrics.RespectConcentration,
            RespectDiversityIndex = metrics.RespectDiversityIndex,
            RespectJson = respectJson
        };
    }

    private static ThanksCoinStepResult BuildEmptyResult(
        SimulationProject project,
        KnowledgeTimelinePoint? previousKnowledgePoint,
        IReadOnlyList<Agent> agents)
    {
        var respectMatrix = LoadRespectMatrix(previousKnowledgePoint?.RespectJson, project, agents);
        var metrics = CalculateRespectMetrics(project, agents, respectMatrix);
        return new ThanksCoinStepResult
        {
            Occurred = false,
            Count = 0,
            HelpCount = 0,
            IdeaCount = 0,
            ChallengeCount = 0,
            BridgeCount = 0,
            RespectJson = SerializeRespectMatrix(respectMatrix),
            AverageRespect = metrics.AverageRespect,
            RespectDensity = metrics.RespectDensity,
            RespectStrongLinks = metrics.RespectStrongLinks,
            RespectWeakLinks = metrics.RespectWeakLinks,
            RespectConcentration = metrics.RespectConcentration,
            RespectDiversityIndex = metrics.RespectDiversityIndex
        };
    }

    private static List<string> ExpandEventTypes(IReadOnlyDictionary<string, int> typeCounts)
    {
        List<string> eventTypes = [];
        foreach (var entry in typeCounts)
        {
            for (var index = 0; index < entry.Value; index++)
            {
                eventTypes.Add(entry.Key);
            }
        }

        return eventTypes;
    }

    private static Dictionary<string, int> AllocateTypeCounts(
        SimulationProject project,
        ActionDistributionSummary actions,
        int totalEvents)
    {
        var helpWeight = 0.40
            + (Math.Clamp(project.CooperationLevel, 0, 1) * 0.15)
            + (actions.SupportOtherRate * 0.10);
        var ideaWeight = 0.25
            + (Math.Clamp(project.LearningOrientationLevel, 0, 1) * 0.25)
            + (actions.ProposeIdeaRate * 0.15);
        var challengeWeight = 0.20
            + (Math.Clamp(project.PsychologicalSafetyLevel, 0, 1) * 0.25)
            + (Math.Clamp(project.ConstructiveCriticismBonus, 0, 1) * 0.20)
            + (Math.Clamp(project.ThanksCoinChallengeBonus, 0, 1) * 0.35)
            + (actions.CriticizeRate * 0.15);
        var bridgeWeight = 0.20
            + (Math.Clamp(project.CrossDomainExposure, 0, 1) * 0.25)
            + (Math.Clamp(project.RewiringSensitivity, 0, 1) * 0.15)
            + (Math.Clamp(project.ThanksCoinDiversityBonus, 0, 1) * 0.35);

        if (project.PsychologicalSafetyLevel < 0.4)
        {
            challengeWeight *= 0.5;
        }

        var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            [ThanksCoinTypes.Help] = Math.Max(0.01, helpWeight),
            [ThanksCoinTypes.Idea] = Math.Max(0.01, ideaWeight),
            [ThanksCoinTypes.Challenge] = Math.Max(0.01, challengeWeight),
            [ThanksCoinTypes.Bridge] = Math.Max(0.01, bridgeWeight)
        };

        var totalWeight = weights.Values.Sum();
        var remaining = totalEvents;
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var fractionalParts = new List<(string Type, double Fraction)>();

        foreach (var entry in weights)
        {
            var exact = (entry.Value / totalWeight) * totalEvents;
            var integerPart = (int)Math.Floor(exact);
            counts[entry.Key] = integerPart;
            remaining -= integerPart;
            fractionalParts.Add((entry.Key, exact - integerPart));
        }

        foreach (var item in fractionalParts
            .OrderByDescending(entry => entry.Fraction)
            .ThenBy(entry => entry.Type, StringComparer.Ordinal))
        {
            if (remaining <= 0)
            {
                break;
            }

            counts[item.Type]++;
            remaining--;
        }

        return counts;
    }

    private static Agent? SelectRecipient(
        string thanksType,
        Agent sender,
        IReadOnlyList<Agent> agents,
        IReadOnlyDictionary<string, double> senderTrustMap,
        IReadOnlyDictionary<string, Dictionary<string, double>> respectMatrix,
        IReadOnlyDictionary<string, double> inboundTrustAverages,
        SimulationProject project)
    {
        var candidates = agents.Where(agent => agent.Id != sender.Id).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var scored = candidates
            .Select(candidate =>
            {
                var trust = senderTrustMap.TryGetValue(candidate.Name, out var trustValue) ? trustValue : 0;
                var respect = GetRespectValue(respectMatrix, sender.Name, candidate.Name);
                var inboundTrust = inboundTrustAverages.TryGetValue(candidate.Name, out var inbound) ? inbound : 0;
                var diversity = 1 - Math.Max(0, trust);

                var score = thanksType switch
                {
                    var value when string.Equals(value, ThanksCoinTypes.Help, StringComparison.OrdinalIgnoreCase)
                        => 0.35 + (trust * 0.45) + (respect * 0.25) + (project.ThanksCoinPopularityBias * inbound * 0.20) + (project.ThanksCoinDiversityBonus * diversity * 0.15),
                    var value when string.Equals(value, ThanksCoinTypes.Idea, StringComparison.OrdinalIgnoreCase)
                        => 0.25 + (respect * 0.40) + (project.LearningOrientationLevel * 0.30) + (project.ThanksCoinDiversityBonus * 0.20) + (project.ThanksCoinPopularityBias * inbound * 0.10),
                    var value when string.Equals(value, ThanksCoinTypes.Challenge, StringComparison.OrdinalIgnoreCase)
                        => 0.20 + (trust * 0.20) + (respect * 0.35) + (project.PsychologicalSafetyLevel * 0.30) + (project.ThanksCoinChallengeBonus * 0.25),
                    _ => 0.15 + (diversity * 0.55) + ((1 - respect) * 0.10) + (project.CrossDomainExposure * 0.15) + (project.ThanksCoinDiversityBonus * 0.25)
                };

                return new { Candidate = candidate, Score = score };
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Candidate.Name, StringComparer.Ordinal)
            .FirstOrDefault();

        return scored?.Candidate;
    }

    private static double ApplyTrustDelta(
        IDictionary<string, double> trustMap,
        string targetName,
        double delta)
    {
        if (delta == 0)
        {
            return 0;
        }

        var before = trustMap.TryGetValue(targetName, out var currentValue) ? currentValue : 0;
        var after = TrustJsonUtility.Clamp(before + delta);
        trustMap[targetName] = after;
        return Math.Round(after - before, 4);
    }

    private static double ApplyRespectDelta(
        IDictionary<string, Dictionary<string, double>> respectMatrix,
        string sourceName,
        string targetName,
        double delta)
    {
        if (string.Equals(sourceName, targetName, StringComparison.OrdinalIgnoreCase) || delta == 0)
        {
            return 0;
        }

        var row = GetOrCreateRow(respectMatrix, sourceName);
        var before = row.TryGetValue(targetName, out var currentValue) ? currentValue : 0;
        var after = Math.Clamp(before + delta, 0, 1);
        row[targetName] = after;
        return Math.Round(after - before, 4);
    }

    private static Dictionary<string, Dictionary<string, double>> LoadRespectMatrix(
        string? respectJson,
        SimulationProject project,
        IReadOnlyList<Agent> agents)
    {
        var matrix = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        var loaded = false;

        if (!string.IsNullOrWhiteSpace(respectJson))
        {
            try
            {
                using var document = JsonDocument.Parse(respectJson);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var sourceProperty in document.RootElement.EnumerateObject())
                    {
                        var row = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                        if (sourceProperty.Value.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var targetProperty in sourceProperty.Value.EnumerateObject())
                            {
                                if (TryReadDouble(targetProperty.Value, out var value))
                                {
                                    row[targetProperty.Name] = Math.Clamp(value, 0, 1);
                                }
                            }
                        }

                        matrix[sourceProperty.Name] = row;
                        loaded = true;
                    }
                }
            }
            catch
            {
                matrix.Clear();
                loaded = false;
            }
        }

        if (!loaded)
        {
            return BuildInitialRespectMatrix(project, agents);
        }

        var initialRespect = CalculateInitialRespect(project);
        foreach (var sourceAgent in agents)
        {
            var row = GetOrCreateRow(matrix, sourceAgent.Name);
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                if (!row.ContainsKey(targetAgent.Name))
                {
                    row[targetAgent.Name] = initialRespect;
                }
            }
        }

        return matrix;
    }

    private static Dictionary<string, Dictionary<string, double>> BuildInitialRespectMatrix(
        SimulationProject project,
        IReadOnlyList<Agent> agents)
    {
        var initialRespect = CalculateInitialRespect(project);
        var matrix = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);

        foreach (var sourceAgent in agents)
        {
            var row = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                row[targetAgent.Name] = initialRespect;
            }

            matrix[sourceAgent.Name] = row;
        }

        return matrix;
    }

    private static double CalculateInitialRespect(SimulationProject project)
    {
        return Math.Clamp(
            0.20
            + (Math.Clamp(project.PsychologicalSafetyLevel, 0, 1) * 0.10)
            + (Math.Clamp(project.LearningOrientationLevel, 0, 1) * 0.05),
            0,
            1);
    }

    private static RespectMetrics CalculateRespectMetrics(
        SimulationProject project,
        IReadOnlyList<Agent> agents,
        IReadOnlyDictionary<string, Dictionary<string, double>> respectMatrix)
    {
        if (agents.Count <= 1)
        {
            return new RespectMetrics(0, 0, 0, 0, 0, 1);
        }

        var totalPairs = 0;
        var totalRespect = 0.0;
        var strongLinks = 0;
        var weakLinks = 0;

        foreach (var sourceAgent in agents)
        {
            var row = respectMatrix.TryGetValue(sourceAgent.Name, out var sourceRow)
                ? sourceRow
                : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                totalPairs++;
                var value = row.TryGetValue(targetAgent.Name, out var respectValue) ? respectValue : CalculateInitialRespect(project);
                totalRespect += value;
                if (value >= 0.70)
                {
                    strongLinks++;
                }

                if (value > 0 && value < 0.30)
                {
                    weakLinks++;
                }
            }
        }

        var averageRespect = Math.Round(totalRespect / totalPairs, 4);
        var respectDensity = Math.Round(strongLinks / (double)totalPairs, 4);
        var respectConcentration = CalculateRespectConcentration(project, agents, respectMatrix);
        var respectDiversityIndex = Math.Round(1 - respectConcentration, 4);
        return new RespectMetrics(averageRespect, respectDensity, strongLinks, weakLinks, respectConcentration, respectDiversityIndex);
    }

    private static double CalculateRespectConcentration(
        SimulationProject project,
        IReadOnlyList<Agent> agents,
        IReadOnlyDictionary<string, Dictionary<string, double>> respectMatrix)
    {
        if (agents.Count == 0)
        {
            return 0;
        }

        var values = agents
            .Select(sourceAgent =>
            {
                var row = respectMatrix.TryGetValue(sourceAgent.Name, out var sourceRow)
                    ? sourceRow
                    : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                var outboundPositiveRespect = agents
                    .Where(targetAgent => targetAgent.Id != sourceAgent.Id)
                    .Select(targetAgent => row.TryGetValue(targetAgent.Name, out var respectValue) ? respectValue : 0)
                    .Where(value => value > 0)
                    .OrderByDescending(value => value)
                    .ToList();

                if (outboundPositiveRespect.Count == 0)
                {
                    return 0.0;
                }

                var totalPositive = outboundPositiveRespect.Sum();
                if (totalPositive <= 0)
                {
                    return 0.0;
                }

                var topCount = Math.Max(1, Math.Min(project.TrustCapacity, outboundPositiveRespect.Count));
                var topSum = outboundPositiveRespect.Take(topCount).Sum();
                return Math.Clamp(topSum / totalPositive, 0, 1);
            })
            .ToList();

        return values.Count == 0 ? 0 : Math.Round(values.Average(), 4);
    }

    private static Dictionary<string, double> BuildInboundTrustAverages(
        IReadOnlyList<Agent> agents,
        IReadOnlyDictionary<int, Dictionary<string, double>> trustMaps)
    {
        var inbound = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
        foreach (var targetAgent in agents)
        {
            inbound[targetAgent.Name] = [];
        }

        foreach (var sourceAgent in agents)
        {
            if (!trustMaps.TryGetValue(sourceAgent.Id, out var trustMap))
            {
                continue;
            }

            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                var trustValue = trustMap.TryGetValue(targetAgent.Name, out var value) ? value : 0;
                inbound[targetAgent.Name].Add(trustValue);
            }
        }

        return inbound.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Count == 0 ? 0 : Math.Round(pair.Value.Average(), 4),
            StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, double> GetOrCreateRow(
        IDictionary<string, Dictionary<string, double>> respectMatrix,
        string sourceName)
    {
        if (!respectMatrix.TryGetValue(sourceName, out var row))
        {
            row = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            respectMatrix[sourceName] = row;
        }

        return row;
    }

    private static double GetRespectValue(
        IReadOnlyDictionary<string, Dictionary<string, double>> respectMatrix,
        string sourceName,
        string targetName)
    {
        if (!respectMatrix.TryGetValue(sourceName, out var row))
        {
            return 0;
        }

        return row.TryGetValue(targetName, out var value) ? value : 0;
    }

    private static bool TryReadDouble(JsonElement element, out double value)
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

    private static string SerializeRespectMatrix(IReadOnlyDictionary<string, Dictionary<string, double>> respectMatrix)
    {
        return JsonSerializer.Serialize(respectMatrix, JsonOptions);
    }

    private static double CalculateConcentration(IReadOnlyDictionary<string, int> receiverCounts, int totalCount)
    {
        if (totalCount <= 0 || receiverCounts.Count == 0)
        {
            return 0;
        }

        return Math.Round(receiverCounts.Values.Max() / (double)totalCount, 4);
    }

    private sealed record RespectMetrics(
        double AverageRespect,
        double RespectDensity,
        int RespectStrongLinks,
        int RespectWeakLinks,
        double RespectConcentration,
        double RespectDiversityIndex);
}

public sealed record ThanksCoinStepResult
{
    public bool Occurred { get; init; }
    public int Count { get; init; }
    public int HelpCount { get; init; }
    public int IdeaCount { get; init; }
    public int ChallengeCount { get; init; }
    public int BridgeCount { get; init; }
    public double RespectDelta { get; init; }
    public double TrustDelta { get; init; }
    public double ReconfigurationDelta { get; init; }
    public double SerendipityDelta { get; init; }
    public double BridgeDelta { get; init; }
    public double CrossDomainExposureDelta { get; init; }
    public double RewiringDelta { get; init; }
    public double PsychologicalSafetyDelta { get; init; }
    public double ThanksConcentration { get; init; }
    public double AverageRespect { get; init; }
    public double RespectDensity { get; init; }
    public int RespectStrongLinks { get; init; }
    public int RespectWeakLinks { get; init; }
    public double RespectConcentration { get; init; }
    public double RespectDiversityIndex { get; init; }
    public string RespectJson { get; init; } = "{}";
}
