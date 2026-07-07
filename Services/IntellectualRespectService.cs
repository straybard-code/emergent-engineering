using System.Text.Json;
using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class IntellectualRespectService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static IntellectualRespectStepResult Apply(
        SimulationProject project,
        IReadOnlyCollection<AgentAction> currentStepActions,
        ThanksCoinStepResult thanksCoinResult,
        KnowledgeTimelinePoint? previousKnowledgePoint,
        int stepNo)
    {
        var agents = project.Agents.OrderBy(agent => agent.Id).ToList();
        if (agents.Count == 0)
        {
            return BuildEmptyResult(project, thanksCoinResult);
        }

        if (!project.EnableIntellectualRespect)
        {
            return BuildEmptyResult(project, thanksCoinResult);
        }

        var trustMaps = agents.ToDictionary(
            agent => agent.Id,
            agent => TrustJsonUtility.Deserialize(agent.TrustJson));

        var matrix = LoadMatrix(previousKnowledgePoint?.IntellectualRespectJson, project, agents);
        var initialRespect = CalculateInitialRespect(project);
        var stepActions = currentStepActions
            .Where(action => action.Agent is not null)
            .ToLookup(action => action.Agent!.Name, StringComparer.OrdinalIgnoreCase);

        var egoPenaltyApplied = CalculateEgoPenalty(project);
        var hierarchyPenaltyApplied = CalculateHierarchyPenalty(project);
        var learningFromOthersScore = 0.0;
        var learnedFromUnexpectedAgentCount = 0;

        foreach (var sourceAgent in agents)
        {
            var row = GetOrCreateRow(matrix, sourceAgent.Name);
            var sourceActions = stepActions[sourceAgent.Name].ToList();
            var sourceTrustMap = trustMaps[sourceAgent.Id];

            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                var current = row.TryGetValue(targetAgent.Name, out var currentRespect) ? currentRespect : initialRespect;
                var trustValue = sourceTrustMap.TryGetValue(targetAgent.Name, out var trust) ? trust : 0;
                var directDelta = 0.0;

                directDelta -= project.IntellectualRespectDecayRate * 0.2;
                directDelta -= egoPenaltyApplied * 0.10;
                directDelta -= hierarchyPenaltyApplied * 0.10;

                foreach (var action in sourceActions)
                {
                    if (!string.IsNullOrWhiteSpace(action.TargetAgentName)
                        && !string.Equals(action.TargetAgentName, targetAgent.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    directDelta += action.Action switch
                    {
                        _ when string.Equals(action.Action, AgentActionType.ProposeIdea, StringComparison.OrdinalIgnoreCase)
                            => project.IntellectualRespectGrowthRate * project.IntellectualRespectDiversitySensitivity * 0.75,
                        _ when string.Equals(action.Action, AgentActionType.Criticize, StringComparison.OrdinalIgnoreCase)
                            => project.PsychologicalSafetyLevel >= 0.5
                                ? project.IntellectualRespectGrowthRate * project.IntellectualRespectChallengeSensitivity * 0.85
                                : -project.IntellectualRespectGrowthRate * 0.10,
                        _ when string.Equals(action.Action, AgentActionType.SupportOther, StringComparison.OrdinalIgnoreCase)
                            => project.IntellectualRespectGrowthRate * project.IntellectualRespectMentorshipSensitivity * 0.55,
                        _ when string.Equals(action.Action, AgentActionType.ShareInfo, StringComparison.OrdinalIgnoreCase)
                            => project.IntellectualRespectGrowthRate * 0.20,
                        _ when string.Equals(action.Action, AgentActionType.AskHelp, StringComparison.OrdinalIgnoreCase)
                            => project.IntellectualRespectGrowthRate * 0.25,
                        _ when string.Equals(action.Action, AgentActionType.WorkAlone, StringComparison.OrdinalIgnoreCase)
                            => -project.IntellectualRespectDecayRate * 0.35,
                        _ when string.Equals(action.Action, AgentActionType.Wait, StringComparison.OrdinalIgnoreCase)
                            => -project.IntellectualRespectDecayRate * 0.20,
                        _ => 0
                    };

                    if (string.Equals(action.Action, AgentActionType.ProposeIdea, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(action.Action, AgentActionType.Criticize, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(action.Action, AgentActionType.SupportOther, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(action.Action, AgentActionType.ShareInfo, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(action.Action, AgentActionType.AskHelp, StringComparison.OrdinalIgnoreCase))
                    {
                        learningFromOthersScore += 0.01;
                    }
                }

                var trustFactor = Clamp01(trustValue);
                var diversityFactor = Clamp01(project.KnowledgeDiversity);
                var crossDomainFactor = Clamp01(project.CrossDomainExposure);
                directDelta += project.IntellectualRespectGrowthRate * 0.15 * (0.5 + diversityFactor + crossDomainFactor);
                directDelta += project.IntellectualRespectGrowthRate * 0.10 * trustFactor;

                var updated = Math.Clamp(current + directDelta, 0, 1);
                row[targetAgent.Name] = updated;
                if (updated > 0.55 && trustValue < 0.35)
                {
                    learnedFromUnexpectedAgentCount++;
                }
            }
        }

        var metrics = CalculateMetrics(project, agents, trustMaps, matrix, previousKnowledgePoint, thanksCoinResult);
        var respectJson = SerializeMatrix(matrix);

        return new IntellectualRespectStepResult
        {
            IntellectualRespectJson = respectJson,
            AverageIntellectualRespect = metrics.AverageIntellectualRespect,
            IntellectualRespectDensity = metrics.IntellectualRespectDensity,
            IntellectualRespectStrongLinks = metrics.IntellectualRespectStrongLinks,
            IntellectualRespectWeakLinks = metrics.IntellectualRespectWeakLinks,
            IntellectualRespectConcentration = metrics.IntellectualRespectConcentration,
            IntellectualRespectDiversityIndex = metrics.IntellectualRespectDiversityIndex,
            MutualMentorshipScore = metrics.MutualMentorshipScore,
            MentorshipLinkCount = metrics.MentorshipLinkCount,
            CrossMentorshipLinkCount = metrics.CrossMentorshipLinkCount,
            MentorshipDiversityIndex = metrics.MentorshipDiversityIndex,
            LearningFromOthersScore = Clamp01(Math.Round(learningFromOthersScore, 4)),
            LearnedFromUnexpectedAgentCount = learnedFromUnexpectedAgentCount,
            IdeaAcceptanceScore = metrics.IdeaAcceptanceScore,
            ChallengeAcceptanceScore = metrics.ChallengeAcceptanceScore,
            IntellectualRespectIdeaAcceptanceComponent = metrics.IntellectualRespectIdeaAcceptanceComponent,
            IntellectualRespectChallengeAcceptanceComponent = metrics.IntellectualRespectChallengeAcceptanceComponent,
            IntellectualRespectReconfigurationComponent = metrics.IntellectualRespectReconfigurationComponent,
            IntellectualRespectSerendipityComponent = metrics.IntellectualRespectSerendipityComponent,
            IntellectualRespectEmergenceComponent = metrics.IntellectualRespectEmergenceComponent,
            EgoPenaltyApplied = egoPenaltyApplied,
            HierarchyPenaltyApplied = hierarchyPenaltyApplied,
            ThanksIdeaAsIntellectualRespectSignal = agents.Count == 0 ? 0 : Math.Round(thanksCoinResult.IdeaCount / (double)agents.Count, 4),
            ThanksChallengeAsIntellectualRespectSignal = agents.Count == 0 ? 0 : Math.Round(thanksCoinResult.ChallengeCount / (double)agents.Count, 4),
            ThanksBridgeAsMentorshipSignal = agents.Count == 0 ? 0 : Math.Round(thanksCoinResult.BridgeCount / (double)agents.Count, 4)
        };
    }

    private static IntellectualRespectStepResult BuildEmptyResult(
        SimulationProject project,
        ThanksCoinStepResult thanksCoinResult)
    {
        var respectJson = JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, double>>(), JsonOptions);
        return new IntellectualRespectStepResult
        {
            IntellectualRespectJson = respectJson,
            AverageIntellectualRespect = 0,
            IntellectualRespectDensity = 0,
            IntellectualRespectStrongLinks = 0,
            IntellectualRespectWeakLinks = 0,
            IntellectualRespectConcentration = 0,
            IntellectualRespectDiversityIndex = 1,
            MutualMentorshipScore = 0,
            MentorshipLinkCount = 0,
            CrossMentorshipLinkCount = 0,
            MentorshipDiversityIndex = 1,
            LearningFromOthersScore = 0,
            LearnedFromUnexpectedAgentCount = 0,
            IdeaAcceptanceScore = 0,
            ChallengeAcceptanceScore = 0,
            IntellectualRespectIdeaAcceptanceComponent = 0,
            IntellectualRespectChallengeAcceptanceComponent = 0,
            IntellectualRespectReconfigurationComponent = 0,
            IntellectualRespectSerendipityComponent = 0,
            IntellectualRespectEmergenceComponent = 0,
            EgoPenaltyApplied = 0,
            HierarchyPenaltyApplied = 0,
            ThanksIdeaAsIntellectualRespectSignal = 0,
            ThanksChallengeAsIntellectualRespectSignal = 0,
            ThanksBridgeAsMentorshipSignal = 0
        };
    }

    private static IntellectualRespectMetrics CalculateMetrics(
        SimulationProject project,
        IReadOnlyList<Agent> agents,
        IReadOnlyDictionary<int, Dictionary<string, double>> trustMaps,
        IReadOnlyDictionary<string, Dictionary<string, double>> matrix,
        KnowledgeTimelinePoint? previousKnowledgePoint,
        ThanksCoinStepResult thanksCoinResult)
    {
        if (agents.Count <= 1)
        {
            return new IntellectualRespectMetrics(0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0);
        }

        var totalPairs = 0;
        var totalRespect = 0.0;
        var strongLinks = 0;
        var weakLinks = 0;
        var mutualLinks = 0;
        var crossMentorshipLinks = 0;
        var unexpectedSourceAgents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceConcentrations = new List<double>();

        foreach (var sourceAgent in agents)
        {
            var row = matrix.TryGetValue(sourceAgent.Name, out var sourceRow)
                ? sourceRow
                : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            var positiveValues = new List<double>();

            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                totalPairs++;
                var respectValue = row.TryGetValue(targetAgent.Name, out var storedValue)
                    ? storedValue
                    : CalculateInitialRespect(project);
                totalRespect += respectValue;
                positiveValues.Add(Math.Max(respectValue, 0));

                if (respectValue >= 0.70)
                {
                    strongLinks++;
                }

                if (respectValue > 0 && respectValue < 0.30)
                {
                    weakLinks++;
                }

                if (respectValue >= 0.60
                    && trustMaps.TryGetValue(sourceAgent.Id, out var trustMap)
                    && (trustMap.TryGetValue(targetAgent.Name, out var trustValue) ? trustValue : 0) < 0.35)
                {
                    crossMentorshipLinks++;
                    unexpectedSourceAgents.Add(sourceAgent.Name);
                }

                if (respectValue >= 0.60
                    && matrix.TryGetValue(targetAgent.Name, out var targetRow)
                    && targetRow.TryGetValue(sourceAgent.Name, out var reverseValue)
                    && reverseValue >= 0.60)
                {
                    mutualLinks++;
                }
            }

            if (positiveValues.Count > 0)
            {
                var totalPositive = positiveValues.Sum();
                if (totalPositive > 0)
                {
                    var topCount = Math.Max(1, Math.Min(project.TrustCapacity, positiveValues.Count));
                    var topSum = positiveValues.OrderByDescending(value => value).Take(topCount).Sum();
                    sourceConcentrations.Add(Math.Clamp(topSum / totalPositive, 0, 1));
                }
            }
        }

        var averageIntellectualRespect = Math.Round(totalRespect / totalPairs, 4);
        var density = Math.Round(strongLinks / (double)totalPairs, 4);
        var concentration = sourceConcentrations.Count == 0 ? 0 : Math.Round(sourceConcentrations.Average(), 4);
        var diversityIndex = Math.Round(1 - concentration, 4);
        var mentorshipDiversityIndex = Math.Round(1 - concentration * 0.85, 4);
        var averageTrust = CalculateAverageTrust(agents, trustMaps);
        var averageRespect = previousKnowledgePoint?.AverageRespect ?? 0;
        var respectDensity = previousKnowledgePoint?.RespectDensity ?? 0;
        var respectDiversityIndex = previousKnowledgePoint?.RespectDiversityIndex ?? 1;

        var ideaAcceptanceScore = Clamp01(Math.Round(
            (averageTrust * 0.20)
            + (project.PsychologicalSafetyLevel * 0.20)
            + (averageIntellectualRespect * 0.35)
            + (diversityIndex * 0.15)
            + (project.LearningOrientationLevel * 0.10),
            4));

        var challengeAcceptanceScore = Clamp01(Math.Round(
            (project.PsychologicalSafetyLevel * 0.25)
            + (averageRespect * 0.15)
            + (averageIntellectualRespect * 0.30)
            + (project.IntellectualRespectChallengeSensitivity * 0.20)
            + (project.ConstructiveCriticismBonus * 0.10),
            4));

        var crossMentorshipNormalized = Normalize(crossMentorshipLinks, totalPairs);
        var unexpectedNormalized = Normalize(unexpectedSourceAgents.Count, agents.Count);
        var mutualMentorshipScore = Clamp01(Math.Round(
            (density * 0.25)
            + (diversityIndex * 0.25)
            + (crossMentorshipNormalized * 0.20)
            + (unexpectedNormalized * 0.15)
            + (project.KnowledgeDiversity * 0.15),
            4));

        var ideaAcceptanceComponent = Clamp01(Math.Round(
            (ideaAcceptanceScore * 0.20)
            + (averageIntellectualRespect * 0.10)
            + (diversityIndex * 0.10),
            4));

        var challengeAcceptanceComponent = Clamp01(Math.Round(
            (challengeAcceptanceScore * 0.25)
            + (averageIntellectualRespect * 0.10)
            + (respectDensity * 0.05),
            4));

        var reconfigurationComponent = Clamp01(Math.Round(
            (averageIntellectualRespect * 0.20)
            + (diversityIndex * 0.20)
            + (ideaAcceptanceScore * 0.20)
            + (challengeAcceptanceScore * 0.20)
            + (mutualMentorshipScore * 0.20),
            4));

        var serendipityComponent = Clamp01(Math.Round(
            (diversityIndex * 0.25)
            + (crossMentorshipNormalized * 0.25)
            + (unexpectedNormalized * 0.20)
            + (reconfigurationComponent * 0.20)
            + (project.CrossDomainExposure * 0.10),
            4));

        var emergenceComponent = Clamp01(Math.Round(
            (averageIntellectualRespect * 0.10)
            + (density * 0.10)
            + (mutualMentorshipScore * 0.20)
            + (reconfigurationComponent * 0.25)
            + (serendipityComponent * 0.25)
            + (ideaAcceptanceScore * 0.10),
            4));

        return new IntellectualRespectMetrics(
            averageIntellectualRespect,
            density,
            strongLinks,
            weakLinks,
            concentration,
            diversityIndex,
            mutualMentorshipScore,
            mutualLinks,
            crossMentorshipLinks,
            mentorshipDiversityIndex,
            ideaAcceptanceScore,
            challengeAcceptanceScore,
            ideaAcceptanceComponent,
            challengeAcceptanceComponent,
            reconfigurationComponent,
            serendipityComponent,
            emergenceComponent);
    }

    private static Dictionary<string, Dictionary<string, double>> LoadMatrix(
        string? json,
        SimulationProject project,
        IReadOnlyList<Agent> agents)
    {
        var matrix = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                foreach (var source in document.RootElement.EnumerateObject())
                {
                    var row = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    if (source.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var target in source.Value.EnumerateObject())
                        {
                            if (target.Value.ValueKind == JsonValueKind.Number && target.Value.TryGetDouble(out var value))
                            {
                                row[target.Name] = Math.Clamp(value, 0, 1);
                            }
                        }
                    }

                    matrix[source.Name] = row;
                }
            }
            catch
            {
                matrix.Clear();
            }
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

    private static Dictionary<string, double> GetOrCreateRow(
        IDictionary<string, Dictionary<string, double>> matrix,
        string sourceName)
    {
        if (!matrix.TryGetValue(sourceName, out var row))
        {
            row = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            matrix[sourceName] = row;
        }

        return row;
    }

    private static double CalculateInitialRespect(SimulationProject project)
    {
        return Math.Clamp(
            project.IntellectualRespectBase
            + (project.KnowledgeDiversity * 0.10)
            + (project.LearningOrientationLevel * 0.05)
            + (project.PsychologicalSafetyLevel * 0.05)
            + (project.CrossDomainExposure * 0.05),
            0,
            1);
    }

    private static double CalculateAverageTrust(
        IReadOnlyList<Agent> agents,
        IReadOnlyDictionary<int, Dictionary<string, double>> trustMaps)
    {
        var values = new List<double>();
        foreach (var sourceAgent in agents)
        {
            if (!trustMaps.TryGetValue(sourceAgent.Id, out var trustMap))
            {
                continue;
            }

            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                values.Add(trustMap.TryGetValue(targetAgent.Name, out var trustValue) ? trustValue : 0);
            }
        }

        return values.Count == 0 ? 0 : Math.Round(values.Average(), 4);
    }

    private static double CalculateEgoPenalty(SimulationProject project)
    {
        return Clamp01(Math.Round(
            project.CompetitionLevel * project.IntellectualRespectEgoPenalty * (1 - project.LearningOrientationLevel),
            4));
    }

    private static double CalculateHierarchyPenalty(SimulationProject project)
    {
        return Clamp01(Math.Round(
            project.ShortTermResultPressureLevel * project.IntellectualRespectHierarchyPenalty * (1 - project.PsychologicalSafetyLevel),
            4));
    }

    private static double Normalize(int value, int denominator)
    {
        if (denominator <= 0)
        {
            return 0;
        }

        return Clamp01(Math.Round(value / (double)denominator, 4));
    }

    private static string SerializeMatrix(IReadOnlyDictionary<string, Dictionary<string, double>> matrix)
    {
        return JsonSerializer.Serialize(matrix, JsonOptions);
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);
}

public sealed record IntellectualRespectStepResult
{
    public string IntellectualRespectJson { get; init; } = "{}";
    public double AverageIntellectualRespect { get; init; }
    public double IntellectualRespectDensity { get; init; }
    public int IntellectualRespectStrongLinks { get; init; }
    public int IntellectualRespectWeakLinks { get; init; }
    public double IntellectualRespectConcentration { get; init; }
    public double IntellectualRespectDiversityIndex { get; init; }
    public double MutualMentorshipScore { get; init; }
    public int MentorshipLinkCount { get; init; }
    public int CrossMentorshipLinkCount { get; init; }
    public double MentorshipDiversityIndex { get; init; }
    public double LearningFromOthersScore { get; init; }
    public int LearnedFromUnexpectedAgentCount { get; init; }
    public double IdeaAcceptanceScore { get; init; }
    public double ChallengeAcceptanceScore { get; init; }
    public double IntellectualRespectIdeaAcceptanceComponent { get; init; }
    public double IntellectualRespectChallengeAcceptanceComponent { get; init; }
    public double IntellectualRespectReconfigurationComponent { get; init; }
    public double IntellectualRespectSerendipityComponent { get; init; }
    public double IntellectualRespectEmergenceComponent { get; init; }
    public double EgoPenaltyApplied { get; init; }
    public double HierarchyPenaltyApplied { get; init; }
    public double ThanksIdeaAsIntellectualRespectSignal { get; init; }
    public double ThanksChallengeAsIntellectualRespectSignal { get; init; }
    public double ThanksBridgeAsMentorshipSignal { get; init; }
}

internal sealed record IntellectualRespectMetrics(
    double AverageIntellectualRespect,
    double IntellectualRespectDensity,
    int IntellectualRespectStrongLinks,
    int IntellectualRespectWeakLinks,
    double IntellectualRespectConcentration,
    double IntellectualRespectDiversityIndex,
    double MutualMentorshipScore,
    int MentorshipLinkCount,
    int CrossMentorshipLinkCount,
    double MentorshipDiversityIndex,
    double IdeaAcceptanceScore,
    double ChallengeAcceptanceScore,
    double IntellectualRespectIdeaAcceptanceComponent,
    double IntellectualRespectChallengeAcceptanceComponent,
    double IntellectualRespectReconfigurationComponent,
    double IntellectualRespectSerendipityComponent,
    double IntellectualRespectEmergenceComponent);
