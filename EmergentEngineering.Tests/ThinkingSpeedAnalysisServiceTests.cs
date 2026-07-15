using System.Text.Json;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Xunit;

namespace EmergentEngineering.Tests;

public sealed class ThinkingSpeedAnalysisServiceTests
{
    [Fact]
    public void CalculateAgentThinkingSpeed_IsDeterministic()
    {
        var project = CreateProject(
            projectId: 10,
            agentCount: 4,
            thinkingSpeedBase: 1.0,
            thinkingSpeedDispersion: 0.5,
            decisionSpeed: 1.0,
            validationSpeed: 1.0,
            trustValue: 0.5,
            psychologicalSafetyLevel: 0.5);
        var agent = project.Agents[0];

        var first = ThinkingSpeedAnalysisService.CalculateAgentThinkingSpeed(project, agent);
        var second = ThinkingSpeedAnalysisService.CalculateAgentThinkingSpeed(project, agent);

        Assert.Equal(first, second, 10);
    }

    [Fact]
    public void EqualThinkingSpeedConfiguration_HasLowIsolationRisk()
    {
        var project = CreateProject(
            projectId: 11,
            agentCount: 5,
            thinkingSpeedBase: 1.0,
            thinkingSpeedDispersion: 0.0,
            decisionSpeed: 1.0,
            validationSpeed: 1.0,
            trustValue: 0.6,
            psychologicalSafetyLevel: 0.6);

        var analysis = ThinkingSpeedAnalysisService.BuildNetworkAnalysis(
            project,
            BuildKnowledgePoint(cognitiveLoad: 0.15, thinkingSpeedMismatch: 0.10, averageRespect: 0.45, averageIntellectualRespect: 0.45, challengeAcceptanceScore: 0.45, mutualMentorshipScore: 0.35));

        Assert.All(analysis.AgentRows, row => Assert.InRange(row.IsolationRisk, 0, 0.35));
    }

    [Fact]
    public void FastAgent_HasHigherIsolationRiskThanPeers()
    {
        var project = CreateProject(
            projectId: 12,
            agentCount: 5,
            thinkingSpeedBase: 10.0,
            thinkingSpeedDispersion: 1.0,
            decisionSpeed: 1.0,
            validationSpeed: 1.0,
            trustValue: 0.2,
            psychologicalSafetyLevel: 0.25);

        var analysis = ThinkingSpeedAnalysisService.BuildNetworkAnalysis(
            project,
            BuildKnowledgePoint(cognitiveLoad: 0.55, thinkingSpeedMismatch: 0.65, averageRespect: 0.10, averageIntellectualRespect: 0.10, challengeAcceptanceScore: 0.10, mutualMentorshipScore: 0.10));

        var fastest = analysis.AgentRows.OrderByDescending(row => row.ThinkingSpeed).First();
        var slowest = analysis.AgentRows.OrderBy(row => row.ThinkingSpeed).First();

        Assert.True(fastest.IsolationRisk >= slowest.IsolationRisk);
        Assert.True(fastest.IsolationRisk >= 0.25);
        Assert.True(fastest.SpeedRole is "Fast" or "Extreme");
    }

    [Fact]
    public void HighDecisionAndValidationSpeed_ReducesIsolationRisk()
    {
        var baselineProject = CreateProject(
            projectId: 13,
            agentCount: 5,
            thinkingSpeedBase: 10.0,
            thinkingSpeedDispersion: 1.0,
            decisionSpeed: 1.0,
            validationSpeed: 1.0,
            trustValue: 0.2,
            psychologicalSafetyLevel: 0.25);

        var mitigatedProject = CreateProject(
            projectId: 13,
            agentCount: 5,
            thinkingSpeedBase: 10.0,
            thinkingSpeedDispersion: 1.0,
            decisionSpeed: 12.0,
            validationSpeed: 11.0,
            trustValue: 0.9,
            psychologicalSafetyLevel: 0.9);

        var context = BuildKnowledgePoint(
            cognitiveLoad: 0.20,
            thinkingSpeedMismatch: 0.20,
            averageRespect: 0.85,
            averageIntellectualRespect: 0.85,
            challengeAcceptanceScore: 0.80,
            mutualMentorshipScore: 0.75,
            unprocessedIdeaCount: 0,
            unvalidatedHypothesisCount: 0);

        var baselineAnalysis = ThinkingSpeedAnalysisService.BuildNetworkAnalysis(baselineProject, context);
        var mitigatedAnalysis = ThinkingSpeedAnalysisService.BuildNetworkAnalysis(mitigatedProject, context);

        var baselineFastest = baselineAnalysis.AgentRows.OrderByDescending(row => row.ThinkingSpeed).First();
        var mitigatedFastest = mitigatedAnalysis.AgentRows.OrderByDescending(row => row.ThinkingSpeed).First();

        Assert.True(mitigatedFastest.IsolationRisk < baselineFastest.IsolationRisk);
    }

    [Fact]
    public void HigherDispersion_IncreasesTrustFriction()
    {
        var source = new Agent
        {
            Id = 1,
            Name = "Alpha",
            Role = "Lead",
            Personality = "Analyst",
            Orientation = "CrossFunctional",
            TrustJson = JsonSerializer.Serialize(new Dictionary<string, double> { ["Beta"] = 0.6 })
        };
        var target = new Agent
        {
            Id = 2,
            Name = "Beta",
            Role = "Engineer",
            Personality = "Builder",
            Orientation = "Ops",
            TrustJson = JsonSerializer.Serialize(new Dictionary<string, double> { ["Alpha"] = 0.6 })
        };

        var lowDispersionProject = CreateProject(20, 2, 1.0, 0.0, 1.0, 1.0, 0.6, 0.7);
        lowDispersionProject.Agents = [source, target];

        var highDispersionProject = CreateProject(20, 2, 1.0, 1.0, 1.0, 1.0, 0.6, 0.7);
        highDispersionProject.Agents = [CloneAgent(source), CloneAgent(target)];

        var context = BuildKnowledgePoint(
            cognitiveLoad: 0.20,
            thinkingSpeedMismatch: 0.25,
            averageRespect: 0.40,
            averageIntellectualRespect: 0.40,
            challengeAcceptanceScore: 0.40,
            mutualMentorshipScore: 0.40,
            unprocessedIdeaCount: 1,
            unvalidatedHypothesisCount: 1);

        var lowPenalty = ThinkingSpeedAnalysisService.CalculateTrustSpeedFrictionPenalty(lowDispersionProject, lowDispersionProject.Agents[0], lowDispersionProject.Agents[1], context);
        var highPenalty = ThinkingSpeedAnalysisService.CalculateTrustSpeedFrictionPenalty(highDispersionProject, highDispersionProject.Agents[0], highDispersionProject.Agents[1], context);

        Assert.True(highPenalty >= lowPenalty);
    }

    [Fact]
    public void HighSafetyAndRespect_MitigateTrustFriction()
    {
        var source = new Agent
        {
            Id = 1,
            Name = "Alpha",
            Role = "Lead",
            Personality = "Analyst",
            Orientation = "CrossFunctional",
            TrustJson = JsonSerializer.Serialize(new Dictionary<string, double> { ["Beta"] = 0.2 })
        };
        var target = new Agent
        {
            Id = 2,
            Name = "Beta",
            Role = "Engineer",
            Personality = "Builder",
            Orientation = "Ops",
            TrustJson = JsonSerializer.Serialize(new Dictionary<string, double> { ["Alpha"] = 0.2 })
        };

        var lowCultureProject = CreateProject(30, 2, 1.0, 1.0, 1.0, 1.0, 0.2, 0.2);
        lowCultureProject.Agents = [source, target];

        var highCultureProject = CreateProject(30, 2, 1.0, 1.0, 1.0, 1.0, 0.9, 0.9);
        highCultureProject.Agents = [CloneAgent(source), CloneAgent(target)];
        ApplyUniformTrust(highCultureProject, 0.95);

        var lowCultureContext = BuildKnowledgePoint(
            cognitiveLoad: 0.40,
            thinkingSpeedMismatch: 0.55,
            averageRespect: 0.10,
            averageIntellectualRespect: 0.10,
            challengeAcceptanceScore: 0.10,
            mutualMentorshipScore: 0.10,
            unprocessedIdeaCount: 2,
            unvalidatedHypothesisCount: 2);

        var highCultureContext = BuildKnowledgePoint(
            cognitiveLoad: 0.40,
            thinkingSpeedMismatch: 0.55,
            averageRespect: 0.90,
            averageIntellectualRespect: 0.90,
            challengeAcceptanceScore: 0.85,
            mutualMentorshipScore: 0.85,
            unprocessedIdeaCount: 2,
            unvalidatedHypothesisCount: 2);

        var lowPenalty = ThinkingSpeedAnalysisService.CalculateTrustSpeedFrictionPenalty(lowCultureProject, lowCultureProject.Agents[0], lowCultureProject.Agents[1], lowCultureContext);
        var highPenalty = ThinkingSpeedAnalysisService.CalculateTrustSpeedFrictionPenalty(highCultureProject, highCultureProject.Agents[0], highCultureProject.Agents[1], highCultureContext);

        Assert.True(highPenalty <= lowPenalty);
    }

    [Fact]
    public void TrustFriction_IsSmall()
    {
        var project = CreateProject(40, 2, 1.0, 1.0, 1.0, 1.0, 0.2, 0.2);
        project.Agents = [
            new Agent
            {
                Id = 1,
                Name = "Alpha",
                Role = "Lead",
                Personality = "Analyst",
                Orientation = "CrossFunctional",
                TrustJson = JsonSerializer.Serialize(new Dictionary<string, double> { ["Beta"] = 0.0 })
            },
            new Agent
            {
                Id = 2,
                Name = "Beta",
                Role = "Engineer",
                Personality = "Builder",
                Orientation = "Ops",
                TrustJson = JsonSerializer.Serialize(new Dictionary<string, double> { ["Alpha"] = 0.0 })
            }
        ];

        var penalty = ThinkingSpeedAnalysisService.CalculateTrustSpeedFrictionPenalty(
            project,
            project.Agents[0],
            project.Agents[1],
            BuildKnowledgePoint(0.20, 0.20, 0.20, 0.20, 0.20, 0.20, 1, 1));

        Assert.InRange(penalty, 0, 0.01);
    }

    [Theory]
    [InlineData(nameof(IThinkingSpeedSettings.EnableThinkingSpeedModel), true)]
    [InlineData(nameof(IThinkingSpeedSettings.ThinkingSpeedBase), true)]
    [InlineData(nameof(IThinkingSpeedSettings.ThinkingSpeedDispersion), true)]
    [InlineData(nameof(IThinkingSpeedSettings.OrganizationalDecisionSpeed), true)]
    [InlineData(nameof(IThinkingSpeedSettings.OrganizationalValidationSpeed), true)]
    [InlineData("InformationSharingLevel", false)]
    public void ThinkingSpeedParameterRecognition_IsAccurate(string parameterName, bool expected)
    {
        Assert.Equal(expected, ThinkingSpeedAnalysisService.UsesThinkingSpeedParameter(parameterName));
    }

    [Fact]
    public void ThinkingSpeedParameterCombination_IgnoresUnrelatedNames()
    {
        Assert.True(ThinkingSpeedAnalysisService.UsesThinkingSpeedParameters("ThinkingSpeedBase", "SomethingElse"));
        Assert.False(ThinkingSpeedAnalysisService.UsesThinkingSpeedParameters("InformationSharingLevel", "CompetitionLevel"));
    }

    private static SimulationProject CreateProject(
        int projectId,
        int agentCount,
        double thinkingSpeedBase,
        double thinkingSpeedDispersion,
        double decisionSpeed,
        double validationSpeed,
        double trustValue,
        double psychologicalSafetyLevel)
    {
        var project = new SimulationProject
        {
            Id = projectId,
            AgentCount = agentCount,
            ThinkingSpeedBase = thinkingSpeedBase,
            ThinkingSpeedDispersion = thinkingSpeedDispersion,
            OrganizationalDecisionSpeed = decisionSpeed,
            OrganizationalValidationSpeed = validationSpeed,
            PsychologicalSafetyLevel = psychologicalSafetyLevel,
            EnableThinkingSpeedModel = true,
            EnableTrustDynamics = true,
            EffectiveTrustThreshold = 0.5
        };

        for (var index = 1; index <= agentCount; index++)
        {
            project.Agents.Add(new Agent
            {
                Id = index,
                SimulationProjectId = projectId,
                Name = $"Agent{index}",
                Role = index == 1 ? "Lead" : "Member",
                Personality = index % 2 == 0 ? "Builder" : "Analyst",
                Orientation = index % 2 == 0 ? "Ops" : "CrossFunctional",
                TrustJson = "{}"
            });
        }

        ApplyUniformTrust(project, trustValue);
        return project;
    }

    private static void ApplyUniformTrust(SimulationProject project, double trustValue)
    {
        foreach (var source in project.Agents)
        {
            var trustMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var target in project.Agents.Where(agent => agent.Id != source.Id))
            {
                trustMap[target.Name] = trustValue;
            }

            source.TrustJson = JsonSerializer.Serialize(trustMap);
        }
    }

    private static Agent CloneAgent(Agent source)
    {
        return new Agent
        {
            Id = source.Id,
            SimulationProjectId = source.SimulationProjectId,
            Name = source.Name,
            Role = source.Role,
            Memory = source.Memory,
            TrustJson = source.TrustJson,
            Personality = source.Personality,
            Orientation = source.Orientation,
            PositionX = source.PositionX,
            PositionY = source.PositionY
        };
    }

    private static KnowledgeTimelinePoint BuildKnowledgePoint(
        double cognitiveLoad,
        double thinkingSpeedMismatch,
        double averageRespect,
        double averageIntellectualRespect,
        double challengeAcceptanceScore,
        double mutualMentorshipScore,
        double unprocessedIdeaCount = 0,
        double unvalidatedHypothesisCount = 0)
    {
        return new KnowledgeTimelinePoint
        {
            CognitiveLoad = cognitiveLoad,
            ThinkingSpeedMismatch = thinkingSpeedMismatch,
            AverageRespect = averageRespect,
            AverageIntellectualRespect = averageIntellectualRespect,
            ChallengeAcceptanceScore = challengeAcceptanceScore,
            MutualMentorshipScore = mutualMentorshipScore,
            UnprocessedIdeaCount = unprocessedIdeaCount,
            UnvalidatedHypothesisCount = unvalidatedHypothesisCount
        };
    }
}
