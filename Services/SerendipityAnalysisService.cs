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
        bool challengeActive,
        double challengeLevel,
        double psychologicalSafetyLevel,
        double workAloneRate)
    {
        var challengeGapPositive = Clamp01(Math.Max(challengeGap, 0));
        var score =
            (Clamp01(explorationScore) * 0.30)
            + (Math.Clamp(knowledgeDiversity, 0, 1) * 0.20)
            + (Math.Clamp(crossDomainExposure, 0, 1) * 0.20)
            + (challengeGapPositive * 0.15)
            + (Math.Clamp(serendipitySensitivity, 0, 1) * 0.15);

        if (challengeActive)
        {
            score += Math.Clamp(challengeLevel, 0, 1) * 0.10;
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
        double constructiveCriticismBonus = 0)
    {
        var constructiveCriticizeRate = psychologicalSafetyLevel >= 0.5
            ? actions.CriticizeRate
            : 0;

        var score =
            (Clamp01(serendipityScore) * 0.35)
            + (actions.ProposeIdeaRate * 0.20)
            + (constructiveCriticizeRate * 0.15)
            + (actions.SupportOtherRate * 0.15)
            + (Math.Clamp(knowledgeRecombinationRate, 0, 1) * 0.15)
            + (psychologicalSafetyLevel >= 0.7 ? actions.CriticizeRate * Math.Clamp(constructiveCriticismBonus, 0, 1) : 0);

        return Clamp01(Math.Round(score, 4));
    }

    private static double Clamp01(double value)
    {
        return Math.Clamp(value, 0, 1);
    }
}
