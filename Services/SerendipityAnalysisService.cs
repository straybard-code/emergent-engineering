using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class SerendipityAnalysisService
{
    public static double CalculateExplorationScore(
        ActionDistributionSummary actions,
        double crossDomainExposure,
        double explorationTendency,
        bool challengeActive,
        double challengeLevel)
    {
        var score =
            (actions.AskHelpRate * 0.25)
            + (actions.ShareInfoRate * 0.20)
            + (actions.ProposeIdeaRate * 0.20)
            + (actions.SupportOtherRate * 0.15)
            + (Math.Clamp(crossDomainExposure, 0, 1) * 0.10)
            + (Math.Clamp(explorationTendency, 0, 1) * 0.10);

        if (challengeActive)
        {
            score += Math.Clamp(challengeLevel, 0, 1) * 0.10;
        }

        return Clamp01(Math.Round(score, 4));
    }

    public static double CalculateSerendipityScore(
        double explorationScore,
        double knowledgeDiversity,
        double crossDomainExposure,
        double challengeGap,
        double serendipitySensitivity,
        double proposeIdeaRate,
        bool challengeActive,
        double challengeLevel,
        double psychologicalSafetyLevel,
        double criticizeRate,
        double competitionLevel,
        double workAloneRate,
        double averageRespect = 0,
        double respectDensity = 0,
        double challengeAcceptanceScore = 0,
        double thanksChallengeRate = 0,
        double thanksBridgeRate = 0,
        double averageIntellectualRespect = 0,
        double intellectualRespectDensity = 0,
        double intellectualRespectDiversityIndex = 0,
        double mutualMentorshipScore = 0)
    {
        var challengeGapPositive = Clamp01(Math.Max(challengeGap, 0));
        var baseScore =
            (Clamp01(explorationScore) * 0.30)
            + (Math.Clamp(knowledgeDiversity, 0, 1) * 0.20)
            + (Math.Clamp(crossDomainExposure, 0, 1) * 0.20)
            + (challengeGapPositive * 0.15)
            + (Math.Clamp(serendipitySensitivity, 0, 1) * 0.15);
        var constructiveCriticismRate = KnowledgeAnalysisService.CalculateConstructiveCriticismRate(criticizeRate, psychologicalSafetyLevel);
        var score =
            (baseScore * 0.85)
            + (psychologicalSafetyLevel * 0.05)
            + (Math.Clamp(proposeIdeaRate, 0, 1) * 0.05)
            + (constructiveCriticismRate * 0.05)
            + (Clamp01(averageRespect) * 0.05)
            + (Clamp01(respectDensity) * 0.03)
            + (Clamp01(challengeAcceptanceScore) * 0.04)
            + (Clamp01(thanksChallengeRate) * 0.03)
            + (Clamp01(thanksBridgeRate) * 0.04)
            + (Clamp01(averageIntellectualRespect) * 0.05)
            + (Clamp01(intellectualRespectDensity) * 0.04)
            + (Clamp01(intellectualRespectDiversityIndex) * 0.05)
            + (Clamp01(mutualMentorshipScore) * 0.05);

        if (challengeActive)
        {
            score += Math.Clamp(challengeLevel, 0, 1) * 0.10;
        }

        if (psychologicalSafetyLevel > 0.9 && competitionLevel < 0.2)
        {
            score -= 0.05;
        }

        if (psychologicalSafetyLevel < 0.3 && workAloneRate > 0.4)
        {
            score -= 0.15;
        }

        return Clamp01(Math.Round(score, 4));
    }

    public static double CalculateKnowledgeRecombinationScore(
        double serendipityScore,
        ActionDistributionSummary actions,
        double psychologicalSafetyLevel,
        double knowledgeRecombinationRate,
        double constructiveCriticismBonus = 0,
        double averageRespect = 0,
        double respectDensity = 0,
        double challengeAcceptanceScore = 0,
        double thanksChallengeRate = 0,
        double thanksBridgeRate = 0,
        double averageIntellectualRespect = 0,
        double intellectualRespectDensity = 0,
        double intellectualRespectDiversityIndex = 0,
        double mutualMentorshipScore = 0)
    {
        var constructiveCriticizeRate = KnowledgeAnalysisService.CalculateConstructiveCriticismRate(actions.CriticizeRate, psychologicalSafetyLevel);
        var baseScore =
            (Clamp01(serendipityScore) * 0.35)
            + (actions.ProposeIdeaRate * 0.20)
            + (constructiveCriticizeRate * 0.15)
            + (actions.SupportOtherRate * 0.15)
            + (Math.Clamp(knowledgeRecombinationRate, 0, 1) * 0.15);

        var score =
            (baseScore * 0.70)
            + (actions.ProposeIdeaRate * 0.10)
            + (actions.ShareInfoRate * 0.08)
            + (actions.AskHelpRate * 0.07)
            + (constructiveCriticizeRate * 0.10)
            + (psychologicalSafetyLevel * 0.05)
            + (psychologicalSafetyLevel >= 0.7 ? actions.CriticizeRate * Math.Clamp(constructiveCriticismBonus, 0, 1) : 0)
            + (Clamp01(averageRespect) * 0.05)
            + (Clamp01(respectDensity) * 0.03)
            + (Clamp01(challengeAcceptanceScore) * 0.04)
            + (Clamp01(thanksChallengeRate) * 0.03)
            + (Clamp01(thanksBridgeRate) * 0.04)
            + (Clamp01(averageIntellectualRespect) * 0.05)
            + (Clamp01(intellectualRespectDensity) * 0.04)
            + (Clamp01(intellectualRespectDiversityIndex) * 0.05)
            + (Clamp01(mutualMentorshipScore) * 0.05);

        return Clamp01(Math.Round(score, 4));
    }

    private static double Clamp01(double value)
    {
        return Math.Clamp(value, 0, 1);
    }
}
