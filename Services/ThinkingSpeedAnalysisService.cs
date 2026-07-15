using System.Security.Cryptography;
using System.Text;
using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class ThinkingSpeedAnalysisService
{
    private static readonly string[] ThinkingSpeedParameterNames =
    [
        nameof(IThinkingSpeedSettings.EnableThinkingSpeedModel),
        nameof(IThinkingSpeedSettings.ThinkingSpeedBase),
        nameof(IThinkingSpeedSettings.ThinkingSpeedDispersion),
        nameof(IThinkingSpeedSettings.OrganizationalDecisionSpeed),
        nameof(IThinkingSpeedSettings.OrganizationalValidationSpeed)
    ];

    public static bool UsesThinkingSpeedParameter(string? parameterName)
    {
        return !string.IsNullOrWhiteSpace(parameterName)
            && ThinkingSpeedParameterNames.Any(name => string.Equals(name, parameterName, StringComparison.OrdinalIgnoreCase));
    }

    public static bool UsesThinkingSpeedParameters(string? firstParameterName, string? secondParameterName)
    {
        return UsesThinkingSpeedParameter(firstParameterName) || UsesThinkingSpeedParameter(secondParameterName);
    }

    public static double CalculateAgentThinkingSpeed(SimulationProject project, Agent agent)
    {
        var baseSpeed = Math.Clamp(project.ThinkingSpeedBase, ThinkingSpeedConstants.MinThinkingSpeed, ThinkingSpeedConstants.MaxThinkingSpeed);
        if (!project.EnableThinkingSpeedModel || project.ThinkingSpeedDispersion <= 0)
        {
            return baseSpeed;
        }

        var stableKey = $"{project.Id}|{project.AgentCount}|{agent.Id}|{agent.Name}|{agent.Role}|{agent.Personality}|{agent.Orientation}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(stableKey));
        var raw = BitConverter.ToUInt64(hash, 8);
        var normalized = raw / (double)ulong.MaxValue;
        var centered = (normalized * 2.0) - 1.0;
        var thinkingSpeed = baseSpeed * (1 + (centered * project.ThinkingSpeedDispersion));
        return Math.Clamp(thinkingSpeed, ThinkingSpeedConstants.MinThinkingSpeed, ThinkingSpeedConstants.MaxThinkingSpeed);
    }

    public static double CalculateThinkingCapacityMultiplier(SimulationProject project, Agent? agent)
    {
        if (agent is null)
        {
            var baseSpeed = Math.Clamp(project.ThinkingSpeedBase, ThinkingSpeedConstants.MinThinkingSpeed, ThinkingSpeedConstants.MaxThinkingSpeed);
            var baseMultiplier = 1 + Math.Log2(Math.Max(0.1, baseSpeed));
            return Math.Clamp(baseMultiplier, ThinkingSpeedConstants.MinThinkingCapacityMultiplier, ThinkingSpeedConstants.MaxThinkingCapacityMultiplier);
        }

        var stableKey = $"{agent.Name}|{agent.Role}|{agent.Personality}|{agent.Orientation}|{project.AgentCount}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(stableKey));
        var raw = BitConverter.ToUInt64(hash, 0);
        var normalized = raw / (double)ulong.MaxValue;
        var centered = (normalized * 2.0) - 1.0;
        var thinkingSpeed = Math.Clamp(
            project.ThinkingSpeedBase * (1 + (centered * project.ThinkingSpeedDispersion)),
            ThinkingSpeedConstants.MinThinkingSpeed,
            ThinkingSpeedConstants.MaxThinkingSpeed);
        var capacityMultiplier = 1 + Math.Log2(Math.Max(0.1, thinkingSpeed));
        return Math.Clamp(capacityMultiplier, ThinkingSpeedConstants.MinThinkingCapacityMultiplier, ThinkingSpeedConstants.MaxThinkingCapacityMultiplier);
    }

    public static ThinkingSpeedNetworkAnalysisResult BuildNetworkAnalysis(
        SimulationProject? project,
        KnowledgeTimelinePoint? knowledgePoint = null,
        NetworkMetricsResult? networkMetrics = null)
    {
        if (project is null || project.Agents.Count == 0)
        {
            return new ThinkingSpeedNetworkAnalysisResult();
        }

        var agents = project.Agents.OrderBy(agent => agent.Id).ToList();
        var trustRows = NetworkMetricsCalculator.CreateRowsFromAgents(agents);
        var speeds = agents.ToDictionary(agent => agent.Id, agent => CalculateAgentThinkingSpeed(project, agent));
        var averageThinkingSpeed = speeds.Count == 0 ? 0 : speeds.Values.Average();
        var minThinkingSpeed = speeds.Count == 0 ? 0 : speeds.Values.Min();
        var maxThinkingSpeed = speeds.Count == 0 ? 0 : speeds.Values.Max();
        var observedDispersion = averageThinkingSpeed <= 0 || speeds.Count <= 1
            ? 0
            : CalculateStandardDeviation(speeds.Values.ToList()) / Math.Max(averageThinkingSpeed, 0.000001);
        var decisionSpeed = Math.Clamp(project.OrganizationalDecisionSpeed, ThinkingSpeedConstants.MinThinkingSpeed, ThinkingSpeedConstants.MaxThinkingSpeed);
        var validationSpeed = Math.Clamp(project.OrganizationalValidationSpeed, ThinkingSpeedConstants.MinThinkingSpeed, ThinkingSpeedConstants.MaxThinkingSpeed);
        var thinkingToDecisionRatio = averageThinkingSpeed / Math.Max(decisionSpeed, 0.000001);
        var thinkingToValidationRatio = averageThinkingSpeed / Math.Max(validationSpeed, 0.000001);
        var cognitiveLoad = Math.Clamp(knowledgePoint?.CognitiveLoad ?? 0, 0, 1);
        var thinkingSpeedMismatch = Math.Clamp(knowledgePoint?.ThinkingSpeedMismatch ?? 0, 0, 1);
        var averageRespect = Math.Clamp(knowledgePoint?.AverageRespect ?? 0, 0, 1);
        var averageIntellectualRespect = Math.Clamp(knowledgePoint?.AverageIntellectualRespect ?? 0, 0, 1);
        var challengeAcceptanceScore = Math.Clamp(knowledgePoint?.ChallengeAcceptanceScore ?? 0, 0, 1);
        var mutualMentorshipScore = Math.Clamp(knowledgePoint?.MutualMentorshipScore ?? 0, 0, 1);
        var networkAverageTrust = networkMetrics?.AverageTrust ?? NetworkMetricsCalculator.CalculateFromAgents(agents, project.EffectiveTrustThreshold).AverageTrust;

        var rows = agents.Select(agent =>
        {
            var speed = speeds[agent.Id];
            var ratioToAverage = speed / Math.Max(averageThinkingSpeed, 0.000001);
            var outgoingTrust = GetOutgoingAverageTrust(trustRows, agent.Id);
            var incomingTrust = GetIncomingAverageTrust(trustRows, agent.Id);
            var isolationRisk = CalculateIsolationRisk(
                project,
                speed,
                averageThinkingSpeed,
                decisionSpeed,
                validationSpeed,
                outgoingTrust,
                incomingTrust,
                observedDispersion,
                thinkingSpeedMismatch,
                cognitiveLoad,
                averageRespect,
                averageIntellectualRespect,
                challengeAcceptanceScore,
                mutualMentorshipScore);

            return new AgentThinkingSpeedStatus
            {
                AgentId = agent.Id,
                AgentName = agent.Name,
                ThinkingSpeed = Math.Round(speed, 4),
                AverageThinkingSpeed = Math.Round(averageThinkingSpeed, 4),
                ThinkingSpeedToAverageRatio = Math.Round(ratioToAverage, 4),
                DecisionSpeedDifference = Math.Round(speed - decisionSpeed, 4),
                ValidationSpeedDifference = Math.Round(speed - validationSpeed, 4),
                SpeedRole = DetermineSpeedRole(ratioToAverage),
                IsolationRisk = Math.Round(isolationRisk, 4),
                OutgoingTrust = Math.Round(outgoingTrust, 4),
                IncomingTrust = Math.Round(incomingTrust, 4)
            };
        })
        .OrderByDescending(item => item.IsolationRisk)
        .ThenByDescending(item => item.ThinkingSpeed)
        .ThenBy(item => item.AgentName)
        .ToList();

        var highIsolationAgents = rows
            .Where(item => item.IsolationRisk >= 0.70)
            .OrderByDescending(item => item.IsolationRisk)
            .ThenByDescending(item => item.ThinkingSpeed)
            .ThenBy(item => item.AgentName)
            .ToList();

        var interpretation = BuildInterpretation(
            project.EnableThinkingSpeedModel,
            averageThinkingSpeed,
            decisionSpeed,
            validationSpeed,
            observedDispersion,
            cognitiveLoad,
            thinkingSpeedMismatch,
            rows,
            averageRespect,
            averageIntellectualRespect,
            challengeAcceptanceScore,
            mutualMentorshipScore,
            networkAverageTrust);

        return new ThinkingSpeedNetworkAnalysisResult
        {
            Enabled = project.EnableThinkingSpeedModel,
            AverageThinkingSpeed = Math.Round(averageThinkingSpeed, 4),
            MinThinkingSpeed = Math.Round(minThinkingSpeed, 4),
            MaxThinkingSpeed = Math.Round(maxThinkingSpeed, 4),
            ObservedThinkingSpeedDispersion = Math.Round(observedDispersion, 4),
            ThinkingSpeedDispersion = Math.Clamp(project.ThinkingSpeedDispersion, 0, 1),
            OrganizationalDecisionSpeed = Math.Round(decisionSpeed, 4),
            OrganizationalValidationSpeed = Math.Round(validationSpeed, 4),
            ThinkingToDecisionRatio = Math.Round(thinkingToDecisionRatio, 4),
            ThinkingToValidationRatio = Math.Round(thinkingToValidationRatio, 4),
            CognitiveLoad = Math.Round(cognitiveLoad, 4),
            ThinkingSpeedMismatch = Math.Round(thinkingSpeedMismatch, 4),
            AverageTrust = Math.Round(networkAverageTrust, 4),
            AgentRows = rows,
            HighIsolationAgents = highIsolationAgents,
            Interpretation = interpretation
        };
    }

    public static double CalculateTrustSpeedFrictionPenalty(
        SimulationProject project,
        Agent sourceAgent,
        Agent targetAgent,
        KnowledgeTimelinePoint? previousKnowledgePoint)
    {
        if (!project.EnableThinkingSpeedModel || project.Agents.Count == 0 || sourceAgent.Id == targetAgent.Id)
        {
            return 0;
        }

        var sourceSpeed = CalculateAgentThinkingSpeed(project, sourceAgent);
        var targetSpeed = CalculateAgentThinkingSpeed(project, targetAgent);
        var faster = Math.Max(sourceSpeed, targetSpeed);
        var slower = Math.Max(Math.Min(sourceSpeed, targetSpeed), 0.000001);
        var speedRatio = faster / slower;
        if (speedRatio <= 1.20)
        {
            return 0;
        }

        var speedDifferencePressure = Math.Clamp((speedRatio - 1.20) / 4.80, 0, 1);
        var backlogPressure = previousKnowledgePoint is null
            ? 0
            : Math.Clamp(
                (previousKnowledgePoint.UnprocessedIdeaCount + previousKnowledgePoint.UnvalidatedHypothesisCount)
                / Math.Max(project.AgentCount * ThinkingSpeedConstants.BacklogCapPerAgent, 1),
                0,
                1);
        var dispersionPressure = Math.Clamp(project.ThinkingSpeedDispersion, 0, 1);
        var safetyPressure = 1 - Math.Clamp(project.PsychologicalSafetyLevel, 0, 1);
        var respectPressure = 1 - Math.Clamp(previousKnowledgePoint?.AverageRespect ?? 0, 0, 1);
        var intellectualRespectPressure = 1 - Math.Clamp(previousKnowledgePoint?.AverageIntellectualRespect ?? 0, 0, 1);
        var challengeAcceptancePressure = 1 - Math.Clamp(previousKnowledgePoint?.ChallengeAcceptanceScore ?? 0, 0, 1);
        var mentorshipPressure = 1 - Math.Clamp(previousKnowledgePoint?.MutualMentorshipScore ?? 0, 0, 1);

        var directionPressure = sourceSpeed >= targetSpeed
            ? (speedDifferencePressure * 0.55)
              + (backlogPressure * 0.25)
              + (safetyPressure * 0.20)
            : (speedDifferencePressure * 0.45)
              + (dispersionPressure * 0.30)
              + (safetyPressure * 0.25);

        var mitigation = Math.Clamp(
            (Math.Clamp(project.PsychologicalSafetyLevel, 0, 1) * 0.24)
            + (Math.Clamp(GetPairTrustSupport(sourceAgent, targetAgent), 0, 1) * 0.24)
            + (Math.Clamp(previousKnowledgePoint?.AverageRespect ?? 0, 0, 1) * 0.16)
            + (Math.Clamp(previousKnowledgePoint?.AverageIntellectualRespect ?? 0, 0, 1) * 0.16)
            + (Math.Clamp(previousKnowledgePoint?.ChallengeAcceptanceScore ?? 0, 0, 1) * 0.10)
            + (Math.Clamp(previousKnowledgePoint?.MutualMentorshipScore ?? 0, 0, 1) * 0.10),
            0,
            1);

        var penalty = Math.Clamp(directionPressure * (1 - (mitigation * 0.70)), 0, 1);
        penalty *= 0.01;

        penalty += Math.Clamp(
            (respectPressure + intellectualRespectPressure + challengeAcceptancePressure + mentorshipPressure) / 40.0,
            0,
            0.003);

        return Math.Clamp(penalty, 0, 0.01);
    }

    private static double CalculateIsolationRisk(
        SimulationProject project,
        double thinkingSpeed,
        double averageThinkingSpeed,
        double decisionSpeed,
        double validationSpeed,
        double outgoingTrust,
        double incomingTrust,
        double observedDispersion,
        double thinkingSpeedMismatch,
        double cognitiveLoad,
        double averageRespect,
        double averageIntellectualRespect,
        double challengeAcceptanceScore,
        double mutualMentorshipScore)
    {
        var speedGap = Math.Clamp((thinkingSpeed / Math.Max(averageThinkingSpeed, 0.000001) - 1) / 8.0, 0, 1);
        var decisionGap = Math.Clamp((thinkingSpeed / Math.Max(decisionSpeed, 0.000001) - 1) / 8.0, 0, 1);
        var validationGap = Math.Clamp((thinkingSpeed / Math.Max(validationSpeed, 0.000001) - 1) / 8.0, 0, 1);
        var trustIsolation = 1 - Math.Clamp((((outgoingTrust + incomingTrust) / 2.0) + 1.0) / 2.0, 0, 1);
        var structuralPressure = Math.Max(speedGap, Math.Max(decisionGap, validationGap));
        var mitigation = Math.Clamp(
            (Math.Clamp(project.PsychologicalSafetyLevel, 0, 1) * 0.24)
            + (averageRespect * 0.18)
            + (averageIntellectualRespect * 0.18)
            + (challengeAcceptanceScore * 0.20)
            + (mutualMentorshipScore * 0.20),
            0,
            1);

        var risk = Math.Clamp(
            (structuralPressure * 0.30)
            + (trustIsolation * 0.25)
            + (Math.Clamp(thinkingSpeedMismatch, 0, 1) * 0.18)
            + (Math.Clamp(cognitiveLoad, 0, 1) * 0.17)
            + (Math.Clamp(observedDispersion, 0, 1) * 0.10),
            0,
            1);

        return Math.Clamp(risk * (1 - (mitigation * 0.60)), 0, 1);
    }

    private static string DetermineSpeedRole(double ratioToAverage)
    {
        if (ratioToAverage < 0.6)
        {
            return "Slow";
        }

        if (ratioToAverage < 1.5)
        {
            return "Standard";
        }

        if (ratioToAverage < 3.0)
        {
            return "Fast";
        }

        return "Extreme";
    }

    private static string BuildInterpretation(
        bool enabled,
        double averageSpeed,
        double decisionSpeed,
        double validationSpeed,
        double observedDispersion,
        double cognitiveLoad,
        double thinkingSpeedMismatch,
        IReadOnlyCollection<AgentThinkingSpeedStatus> rows,
        double averageRespect,
        double averageIntellectualRespect,
        double challengeAcceptanceScore,
        double mutualMentorshipScore,
        double averageTrust)
    {
        if (!enabled)
        {
            return "思考速度モデルは無効です。現状はベース速度中心の状態として解釈できます。";
        }

        List<string> comments = [];
        var averageSpeedToDecision = averageSpeed / Math.Max(decisionSpeed, 0.000001);
        var averageSpeedToValidation = averageSpeed / Math.Max(validationSpeed, 0.000001);
        var highIsolation = rows.Any(item => item.IsolationRisk >= 0.70);
        var mitigatedByCulture = averageRespect >= 0.50
            || averageIntellectualRespect >= 0.50
            || challengeAcceptanceScore >= 0.55
            || mutualMentorshipScore >= 0.40
            || averageTrust >= 0.35;

        if (Math.Abs(averageSpeedToDecision - 1) <= 0.20
            && Math.Abs(averageSpeedToValidation - 1) <= 0.20
            && cognitiveLoad < 0.45
            && thinkingSpeedMismatch < 0.35)
        {
            comments.Add("平均思考速度が組織処理速度と概ね一致しています。思考生成と意思決定・検証の速度差は小さく、速度面では安定した状態です。");
        }

        if (averageSpeedToDecision > 1.20 && cognitiveLoad >= 0.35)
        {
            comments.Add("思考生成速度が意思決定速度を上回っています。提案処理が追いつかず、未処理提案が蓄積する可能性があります。");
        }

        if (averageSpeedToValidation > 1.20 && cognitiveLoad >= 0.35)
        {
            comments.Add("思考生成速度が検証速度を上回っています。仮説生成に対して検証能力が不足しており、未検証仮説の蓄積が起きています。");
        }

        if (observedDispersion >= 0.35)
        {
            comments.Add("エージェント間の思考速度差が大きくなっています。高速エージェントと低速エージェント間で認識速度の摩擦が発生する可能性があります。");
        }

        if (observedDispersion >= 0.35 && mitigatedByCulture)
        {
            comments.Add("思考速度差は大きいものの、知的敬意または心理的安全性が高く、速度差による摩擦は緩和されている可能性があります。");
        }

        if (highIsolation)
        {
            comments.Add("高速エージェントのIsolation Riskが高くなっています。提案生成能力は高い一方、組織処理速度またはTrust Networkとの不整合が発生している示唆があります。");
        }

        if (comments.Count == 0)
        {
            comments.Add("思考生成速度と組織の意思決定・検証速度の整合は概ね保たれています。");
        }

        return string.Join(" ", comments);
    }

    private static double GetOutgoingAverageTrust(IReadOnlyCollection<NetworkTrustEdgeRef> rows, int agentId)
    {
        var values = rows
            .Where(row => row.SourceAgentId == agentId)
            .Select(row => row.TrustValue)
            .ToList();
        return values.Count == 0 ? 0 : values.Average();
    }

    private static double GetIncomingAverageTrust(IReadOnlyCollection<NetworkTrustEdgeRef> rows, int agentId)
    {
        var values = rows
            .Where(row => row.TargetAgentId == agentId)
            .Select(row => row.TrustValue)
            .ToList();
        return values.Count == 0 ? 0 : values.Average();
    }

    private static double GetPairTrustSupport(Agent sourceAgent, Agent targetAgent)
    {
        var sourceTrust = TrustJsonUtility.Deserialize(sourceAgent.TrustJson).GetValueOrDefault(targetAgent.Name, 0);
        var targetTrust = TrustJsonUtility.Deserialize(targetAgent.TrustJson).GetValueOrDefault(sourceAgent.Name, 0);
        var normalized = ((sourceTrust + targetTrust) / 2.0 + 1.0) / 2.0;
        return Math.Clamp(normalized, 0, 1);
    }

    private static double CalculateStandardDeviation(IReadOnlyCollection<double> values)
    {
        if (values.Count <= 1)
        {
            return 0;
        }

        var average = values.Average();
        var variance = values.Sum(value => Math.Pow(value - average, 2)) / values.Count;
        return Math.Sqrt(Math.Max(variance, 0));
    }
}

public sealed record AgentThinkingSpeedStatus
{
    public int AgentId { get; init; }
    public string AgentName { get; init; } = "";
    public double ThinkingSpeed { get; init; }
    public double AverageThinkingSpeed { get; init; }
    public double ThinkingSpeedToAverageRatio { get; init; }
    public double DecisionSpeedDifference { get; init; }
    public double ValidationSpeedDifference { get; init; }
    public string SpeedRole { get; init; } = "Standard";
    public double IsolationRisk { get; init; }
    public double OutgoingTrust { get; init; }
    public double IncomingTrust { get; init; }
}

public sealed record ThinkingSpeedNetworkAnalysisResult
{
    public bool Enabled { get; init; }
    public double AverageThinkingSpeed { get; init; }
    public double MinThinkingSpeed { get; init; }
    public double MaxThinkingSpeed { get; init; }
    public double ThinkingSpeedDispersion { get; init; }
    public double ObservedThinkingSpeedDispersion { get; init; }
    public double OrganizationalDecisionSpeed { get; init; }
    public double OrganizationalValidationSpeed { get; init; }
    public double ThinkingToDecisionRatio { get; init; }
    public double ThinkingToValidationRatio { get; init; }
    public double CognitiveLoad { get; init; }
    public double ThinkingSpeedMismatch { get; init; }
    public double AverageTrust { get; init; }
    public IReadOnlyList<AgentThinkingSpeedStatus> AgentRows { get; init; } = [];
    public IReadOnlyList<AgentThinkingSpeedStatus> HighIsolationAgents { get; init; } = [];
    public string Interpretation { get; init; } = "-";
    public bool HasData => AgentRows.Count > 0;
}
