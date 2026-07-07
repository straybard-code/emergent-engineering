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
        ActionDistributionSummary actions,
        KnowledgeTimelinePoint? previousKnowledgePoint,
        int stepNo)
    {
        var agents = project.Agents.OrderBy(agent => agent.Id).ToList();
        if (agents.Count == 0)
        {
            return BuildEmptyResult(project, agents);
        }

        if (!project.EnableIntellectualRespect)
        {
            return new IntellectualRespectStepResult
            {
                IntellectualRespectJson = ""
            };
        }

        var matrix = LoadMatrix(previousKnowledgePoint?.IntellectualRespectJson, project, agents);
        var trustMaps = agents.ToDictionary(agent => agent.Id, agent => TrustJsonUtility.Deserialize(agent.TrustJson));
        var averageTrust = agents.Count == 0
            ? 0
            : Math.Round(agents.Average(agent => TrustJsonUtility.CalculateAverage(agent.TrustJson)), 4);

        var initialRespect = KnowledgeAnalysisService.CalculateInitialIntellectualRespect(project);
        var constructiveCriticismRate = KnowledgeAnalysisService.CalculateConstructiveCriticismRate(
            actions.CriticizeRate,
            project.PsychologicalSafetyLevel);
        var ideaSignal = Math.Clamp(actions.ProposeIdeaRate * Math.Clamp(project.IntellectualRespectMentorshipSensitivity, 0, 1), 0, 1);
        var challengeSignal = Math.Clamp(constructiveCriticismRate * Math.Clamp(project.IntellectualRespectChallengeSensitivity, 0, 1), 0, 1);
        var bridgeSignal = Math.Clamp(
            ((Math.Clamp(project.CrossDomainExposure, 0, 1) * 0.5) + (actions.SupportOtherRate * 0.25))
            * Math.Clamp(project.IntellectualRespectDiversitySensitivity, 0, 1),
            0,
            1);
        var mentorshipSignal = Math.Clamp(
            (ideaSignal * 0.35)
            + (challengeSignal * 0.35)
            + (bridgeSignal * 0.30),
            0,
            1);
        var decay = Math.Clamp(project.IntellectualRespectDecayRate, 0, 1);
        var growth = Math.Clamp(project.IntellectualRespectGrowthRate, 0, 1);
        var egoPenaltyApplied = Math.Clamp(
            Math.Max(0, actions.WorkAloneRate) * Math.Clamp(project.IntellectualRespectEgoPenalty, 0, 1) * (1 - Math.Clamp(project.LearningOrientationLevel, 0, 1)),
            0,
            1);
        var hierarchyPenaltyApplied = Math.Clamp(
            Math.Max(0, project.ShortTermResultPressureLevel) * Math.Clamp(project.IntellectualRespectHierarchyPenalty, 0, 1) * (1 - Math.Clamp(project.PsychologicalSafetyLevel, 0, 1)),
            0,
            1);

        foreach (var sourceAgent in agents)
        {
            var row = GetOrCreateRow(matrix, sourceAgent.Name);
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                var before = row.TryGetValue(targetAgent.Name, out var currentValue) ? currentValue : initialRespect;
                var pairDiversity = CalculatePairDiversity(sourceAgent, targetAgent);
                var trustValue = trustMaps[sourceAgent.Id].TryGetValue(targetAgent.Name, out var trust) ? trust : 0;

                var delta = (growth * (0.10 + (pairDiversity * 0.40)))
                    + (ideaSignal * (0.20 + (pairDiversity * 0.25)))
                    + (challengeSignal * (0.20 + (pairDiversity * 0.25)))
                    + (bridgeSignal * (0.25 + (pairDiversity * 0.35)))
                    + (Math.Clamp(previousKnowledgePoint?.AverageIntellectualRespect ?? initialRespect, 0, 1) * 0.05)
                    + (trust * 0.03)
                    - (decay * (1 - pairDiversity) * 0.5)
                    - (egoPenaltyApplied * 0.04)
                    - (hierarchyPenaltyApplied * 0.05);

                if (pairDiversity >= 0.50)
                {
                    delta += mentorshipSignal * 0.10;
                }

                var after = Math.Clamp(before + delta, 0, 1);
                row[targetAgent.Name] = Math.Round(after, 4);
            }
        }

        var metrics = CalculateMetrics(matrix, agents);
        var strongLinkCount = metrics.RespectStrongLinks;
        var crossMentorshipLinkCount = CountCrossMentorshipLinks(matrix, agents, trustMaps);
        var learnedFromUnexpectedAgentCount = CountUnexpectedMentorshipLinks(matrix, agents, trustMaps);
        var mentorshipDiversityIndex = metrics.IntellectualRespectDiversityIndex;
        var learningFromOthersScore = Math.Round(
            (metrics.AverageIntellectualRespect * 0.45)
            + (metrics.IntellectualRespectDiversityIndex * 0.25)
            + (metrics.IntellectualRespectDensity * 0.30),
            4);
        var mutualMentorshipScore = KnowledgeAnalysisService.CalculateMutualMentorshipScore(
            metrics.IntellectualRespectDensity,
            metrics.IntellectualRespectDiversityIndex,
            crossMentorshipLinkCount,
            learnedFromUnexpectedAgentCount,
            project.KnowledgeDiversity,
            agents.Count);

        return new IntellectualRespectStepResult
        {
            AverageIntellectualRespect = metrics.AverageIntellectualRespect,
            IntellectualRespectDensity = metrics.IntellectualRespectDensity,
            IntellectualRespectStrongLinks = metrics.RespectStrongLinks,
            IntellectualRespectWeakLinks = metrics.RespectWeakLinks,
            IntellectualRespectConcentration = metrics.RespectConcentration,
            IntellectualRespectDiversityIndex = metrics.IntellectualRespectDiversityIndex,
            MutualMentorshipScore = mutualMentorshipScore,
            MentorshipLinkCount = strongLinkCount,
            CrossMentorshipLinkCount = crossMentorshipLinkCount,
            MentorshipDiversityIndex = mentorshipDiversityIndex,
            LearningFromOthersScore = learningFromOthersScore,
            LearnedFromUnexpectedAgentCount = learnedFromUnexpectedAgentCount,
            IntellectualRespectJson = SerializeMatrix(matrix),
            IdeaAcceptanceScore = KnowledgeAnalysisService.CalculateIdeaAcceptanceScore(
                averageTrust,
                project.PsychologicalSafetyLevel,
                metrics.AverageIntellectualRespect,
                metrics.IntellectualRespectDiversityIndex,
                project.LearningOrientationLevel),
            ChallengeAcceptanceScore = KnowledgeAnalysisService.CalculateChallengeAcceptanceScore(
                project.PsychologicalSafetyLevel,
                previousKnowledgePoint?.AverageRespect ?? 0,
                metrics.AverageIntellectualRespect,
                project.IntellectualRespectChallengeSensitivity,
                challengeSignal,
                project.ConstructiveCriticismBonus),
            IntellectualRespectIdeaAcceptanceComponent = Math.Round((ideaSignal * 0.20) + (metrics.AverageIntellectualRespect * 0.15), 4),
            IntellectualRespectChallengeAcceptanceComponent = Math.Round((challengeSignal * 0.20) + (metrics.IntellectualRespectDensity * 0.15), 4),
            IntellectualRespectReconfigurationComponent = 0,
            IntellectualRespectSerendipityComponent = 0,
            IntellectualRespectEmergenceComponent = 0,
            EgoPenaltyApplied = Math.Round(egoPenaltyApplied, 4),
            HierarchyPenaltyApplied = Math.Round(hierarchyPenaltyApplied, 4),
            ThanksIdeaAsIntellectualRespectSignal = Math.Round(ideaSignal, 4),
            ThanksChallengeAsIntellectualRespectSignal = Math.Round(challengeSignal, 4),
            ThanksBridgeAsMentorshipSignal = Math.Round(bridgeSignal, 4)
        };
    }

    public static IntellectualRespectStepResult BuildDerivedScores(
        IntellectualRespectStepResult stepResult,
        double averageRespect,
        double respectDensity,
        double challengeAcceptanceScore,
        double crossDomainExposure)
    {
        var reconfigurationComponent = KnowledgeAnalysisService.CalculateIntellectualRespectReconfigurationComponent(
            stepResult.AverageIntellectualRespect,
            stepResult.IntellectualRespectDensity,
            stepResult.IdeaAcceptanceScore,
            challengeAcceptanceScore,
            stepResult.MutualMentorshipScore);

        var crossMentorshipNormalized = stepResult.MentorshipLinkCount <= 0
            ? 0
            : Math.Clamp(stepResult.CrossMentorshipLinkCount / (double)Math.Max(1, stepResult.MentorshipLinkCount), 0, 1);
        var unexpectedNormalized = stepResult.LearnedFromUnexpectedAgentCount <= 0
            ? 0
            : Math.Clamp(stepResult.LearnedFromUnexpectedAgentCount / (double)Math.Max(1, stepResult.MentorshipLinkCount), 0, 1);
        var serendipityComponent = KnowledgeAnalysisService.CalculateIntellectualRespectSerendipityComponent(
            stepResult.IntellectualRespectDiversityIndex,
            crossMentorshipNormalized,
            unexpectedNormalized,
            reconfigurationComponent,
            crossDomainExposure);

        var emergenceComponent = KnowledgeAnalysisService.CalculateIntellectualRespectEmergenceComponent(
            stepResult.AverageIntellectualRespect,
            stepResult.IntellectualRespectDensity,
            stepResult.MutualMentorshipScore,
            reconfigurationComponent,
            serendipityComponent,
            stepResult.IdeaAcceptanceScore);

        stepResult.IntellectualRespectReconfigurationComponent = reconfigurationComponent;
        stepResult.IntellectualRespectSerendipityComponent = serendipityComponent;
        stepResult.IntellectualRespectEmergenceComponent = emergenceComponent;
        return stepResult;
    }

    private static IntellectualRespectStepResult BuildEmptyResult(SimulationProject project, IReadOnlyList<Agent> agents)
    {
        var matrix = BuildInitialMatrix(project, agents);
        var metrics = CalculateMetrics(matrix, agents);
        return new IntellectualRespectStepResult
        {
            AverageIntellectualRespect = metrics.AverageIntellectualRespect,
            IntellectualRespectDensity = metrics.IntellectualRespectDensity,
            IntellectualRespectStrongLinks = metrics.RespectStrongLinks,
            IntellectualRespectWeakLinks = metrics.RespectWeakLinks,
            IntellectualRespectConcentration = metrics.RespectConcentration,
            IntellectualRespectDiversityIndex = metrics.IntellectualRespectDiversityIndex,
            IntellectualRespectJson = SerializeMatrix(matrix)
        };
    }

    private static Dictionary<string, Dictionary<string, double>> LoadMatrix(
        string? json,
        SimulationProject project,
        IReadOnlyList<Agent> agents)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return BuildInitialMatrix(project, agents);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return BuildInitialMatrix(project, agents);
            }

            var matrix = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceProperty in document.RootElement.EnumerateObject())
            {
                var row = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                if (sourceProperty.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var targetProperty in sourceProperty.Value.EnumerateObject())
                    {
                        if (targetProperty.Value.ValueKind == JsonValueKind.Number && targetProperty.Value.TryGetDouble(out var value))
                        {
                            row[targetProperty.Name] = Math.Clamp(value, 0, 1);
                        }
                    }
                }

                matrix[sourceProperty.Name] = row;
            }

            return EnsureAllPairs(matrix, project, agents);
        }
        catch
        {
            return BuildInitialMatrix(project, agents);
        }
    }

    private static Dictionary<string, Dictionary<string, double>> BuildInitialMatrix(
        SimulationProject project,
        IReadOnlyList<Agent> agents)
    {
        var matrix = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        var baseRespect = KnowledgeAnalysisService.CalculateInitialIntellectualRespect(project);
        foreach (var sourceAgent in agents)
        {
            var row = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                row[targetAgent.Name] = baseRespect;
            }

            matrix[sourceAgent.Name] = row;
        }

        return matrix;
    }

    private static Dictionary<string, Dictionary<string, double>> EnsureAllPairs(
        Dictionary<string, Dictionary<string, double>> matrix,
        SimulationProject project,
        IReadOnlyList<Agent> agents)
    {
        var baseRespect = KnowledgeAnalysisService.CalculateInitialIntellectualRespect(project);
        foreach (var sourceAgent in agents)
        {
            var row = GetOrCreateRow(matrix, sourceAgent.Name);
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                if (!row.ContainsKey(targetAgent.Name))
                {
                    row[targetAgent.Name] = baseRespect;
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

    private static double CalculatePairDiversity(Agent source, Agent target)
    {
        var score = 0.0;
        score += !string.Equals(source.Role, target.Role, StringComparison.OrdinalIgnoreCase) ? 0.35 : 0;
        score += !string.Equals(source.Personality, target.Personality, StringComparison.OrdinalIgnoreCase) ? 0.25 : 0;
        score += !string.Equals(source.Orientation, target.Orientation, StringComparison.OrdinalIgnoreCase) ? 0.25 : 0;
        score += (Math.Abs(source.PositionX - target.PositionX) + Math.Abs(source.PositionY - target.PositionY)) > 0.5 ? 0.15 : 0;
        return Math.Clamp(score, 0, 1);
    }

    private static IntellectualRespectMetrics CalculateMetrics(
        IReadOnlyDictionary<string, Dictionary<string, double>> matrix,
        IReadOnlyList<Agent> agents)
    {
        var values = new List<double>();
        var strongLinks = 0;
        var weakLinks = 0;
        var concentration = 0.0;
        var rowAverages = new List<double>();

        foreach (var sourceAgent in agents)
        {
            var row = matrix.TryGetValue(sourceAgent.Name, out var rowValues)
                ? rowValues
                : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var rowList = new List<double>();
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                var value = row.TryGetValue(targetAgent.Name, out var current) ? current : 0;
                values.Add(value);
                rowList.Add(value);
                if (value >= 0.60)
                {
                    strongLinks++;
                }
                else if (value > 0 && value < 0.35)
                {
                    weakLinks++;
                }
            }

            if (rowList.Count > 0)
            {
                var average = rowList.Average();
                rowAverages.Add(average);
                concentration = Math.Max(concentration, average);
            }
        }

        var averageRespect = values.Count == 0 ? 0 : Math.Round(values.Average(), 4);
        var density = values.Count == 0 ? 0 : Math.Round(values.Count(value => value >= 0.45) / (double)values.Count, 4);
        var diversityIndex = rowAverages.Count == 0
            ? 0
            : Math.Clamp(Math.Round(1 - rowAverages.Sum(value => value * value), 4), 0, 1);

        return new IntellectualRespectMetrics(
            averageRespect,
            density,
            strongLinks,
            weakLinks,
            Math.Round(concentration, 4),
            diversityIndex);
    }

    private static int CountCrossMentorshipLinks(
        IReadOnlyDictionary<string, Dictionary<string, double>> matrix,
        IReadOnlyList<Agent> agents,
        IReadOnlyDictionary<int, Dictionary<string, double>> trustMaps)
    {
        var count = 0;
        foreach (var sourceAgent in agents)
        {
            var row = matrix.TryGetValue(sourceAgent.Name, out var rowValues)
                ? rowValues
                : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                var value = row.TryGetValue(targetAgent.Name, out var current) ? current : 0;
                var trust = trustMaps.TryGetValue(sourceAgent.Id, out var trustMap) && trustMap.TryGetValue(targetAgent.Name, out var trustValue)
                    ? trustValue
                    : 0;
                if (value >= 0.60 && trust < 0.35 && CalculatePairDiversity(sourceAgent, targetAgent) >= 0.50)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountUnexpectedMentorshipLinks(
        IReadOnlyDictionary<string, Dictionary<string, double>> matrix,
        IReadOnlyList<Agent> agents,
        IReadOnlyDictionary<int, Dictionary<string, double>> trustMaps)
    {
        var count = 0;
        foreach (var sourceAgent in agents)
        {
            var row = matrix.TryGetValue(sourceAgent.Name, out var rowValues)
                ? rowValues
                : new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var targetAgent in agents.Where(agent => agent.Id != sourceAgent.Id))
            {
                var value = row.TryGetValue(targetAgent.Name, out var current) ? current : 0;
                var trust = trustMaps.TryGetValue(sourceAgent.Id, out var trustMap) && trustMap.TryGetValue(targetAgent.Name, out var trustValue)
                    ? trustValue
                    : 0;
                if (value >= 0.55 && trust < 0.30)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static string SerializeMatrix(IReadOnlyDictionary<string, Dictionary<string, double>> matrix)
    {
        return JsonSerializer.Serialize(matrix, JsonOptions);
    }

    private sealed record IntellectualRespectMetrics(
        double AverageIntellectualRespect,
        double IntellectualRespectDensity,
        int RespectStrongLinks,
        int RespectWeakLinks,
        double RespectConcentration,
        double IntellectualRespectDiversityIndex);

    public sealed class IntellectualRespectStepResult
    {
        public double AverageIntellectualRespect { get; set; }
        public double IntellectualRespectDensity { get; set; }
        public int IntellectualRespectStrongLinks { get; set; }
        public int IntellectualRespectWeakLinks { get; set; }
        public double IntellectualRespectConcentration { get; set; }
        public double IntellectualRespectDiversityIndex { get; set; }
        public double MutualMentorshipScore { get; set; }
        public int MentorshipLinkCount { get; set; }
        public int CrossMentorshipLinkCount { get; set; }
        public double MentorshipDiversityIndex { get; set; }
        public double LearningFromOthersScore { get; set; }
        public int LearnedFromUnexpectedAgentCount { get; set; }
        public string IntellectualRespectJson { get; set; } = "";
        public double IdeaAcceptanceScore { get; set; }
        public double ChallengeAcceptanceScore { get; set; }
        public double IntellectualRespectIdeaAcceptanceComponent { get; set; }
        public double IntellectualRespectChallengeAcceptanceComponent { get; set; }
        public double IntellectualRespectReconfigurationComponent { get; set; }
        public double IntellectualRespectSerendipityComponent { get; set; }
        public double IntellectualRespectEmergenceComponent { get; set; }
        public double EgoPenaltyApplied { get; set; }
        public double HierarchyPenaltyApplied { get; set; }
        public double ThanksIdeaAsIntellectualRespectSignal { get; set; }
        public double ThanksChallengeAsIntellectualRespectSignal { get; set; }
        public double ThanksBridgeAsMentorshipSignal { get; set; }
    }
}
