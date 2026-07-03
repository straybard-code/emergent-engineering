using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class ActionDistributionCalculator
{
    public const string OtherAction = "Other";

    public static Dictionary<int, ActionDistributionSummary> BuildByStep<T>(
        IEnumerable<T> items,
        Func<T, int> stepSelector,
        Func<T, string?> actionSelector)
    {
        return items
            .GroupBy(stepSelector)
            .ToDictionary(
                group => group.Key,
                group => Calculate(group.Select(actionSelector)));
    }

    public static string Normalize(string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return OtherAction;
        }

        if (string.Equals(action, AgentActionType.ShareInfo, StringComparison.OrdinalIgnoreCase))
        {
            return AgentActionType.ShareInfo;
        }

        if (string.Equals(action, AgentActionType.AskHelp, StringComparison.OrdinalIgnoreCase))
        {
            return AgentActionType.AskHelp;
        }

        if (string.Equals(action, AgentActionType.ProposeIdea, StringComparison.OrdinalIgnoreCase))
        {
            return AgentActionType.ProposeIdea;
        }

        if (string.Equals(action, AgentActionType.Criticize, StringComparison.OrdinalIgnoreCase))
        {
            return AgentActionType.Criticize;
        }

        if (string.Equals(action, AgentActionType.WorkAlone, StringComparison.OrdinalIgnoreCase))
        {
            return AgentActionType.WorkAlone;
        }

        if (string.Equals(action, AgentActionType.SupportOther, StringComparison.OrdinalIgnoreCase))
        {
            return AgentActionType.SupportOther;
        }

        if (string.Equals(action, AgentActionType.Wait, StringComparison.OrdinalIgnoreCase))
        {
            return AgentActionType.Wait;
        }

        return OtherAction;
    }

    public static string GetDisplayLabel(string? action)
    {
        return Normalize(action) switch
        {
            AgentActionType.ShareInfo => "\u60C5\u5831\u5171\u6709",
            AgentActionType.AskHelp => "\u76F8\u8AC7",
            AgentActionType.ProposeIdea => "\u30A2\u30A4\u30C7\u30A2\u63D0\u6848",
            AgentActionType.Criticize => "\u6279\u5224",
            AgentActionType.WorkAlone => "\u5358\u72EC\u4F5C\u696D",
            AgentActionType.SupportOther => "\u652F\u63F4",
            AgentActionType.Wait => "\u5F85\u6A5F",
            _ => OtherAction
        };
    }

    public static ActionDistributionSummary Calculate(IEnumerable<string?> actions)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [AgentActionType.ShareInfo] = 0,
            [AgentActionType.AskHelp] = 0,
            [AgentActionType.ProposeIdea] = 0,
            [AgentActionType.Criticize] = 0,
            [AgentActionType.WorkAlone] = 0,
            [AgentActionType.SupportOther] = 0,
            [AgentActionType.Wait] = 0,
            [OtherAction] = 0
        };

        var totalCount = 0;
        foreach (var action in actions)
        {
            totalCount++;
            counts[Normalize(action)]++;
        }

        return new ActionDistributionSummary
        {
            TotalCount = totalCount,
            ShareInfoCount = counts[AgentActionType.ShareInfo],
            AskHelpCount = counts[AgentActionType.AskHelp],
            ProposeIdeaCount = counts[AgentActionType.ProposeIdea],
            CriticizeCount = counts[AgentActionType.Criticize],
            WorkAloneCount = counts[AgentActionType.WorkAlone],
            SupportOtherCount = counts[AgentActionType.SupportOther],
            WaitCount = counts[AgentActionType.Wait],
            OtherCount = counts[OtherAction],
            ShareInfoRate = CalculateRate(counts[AgentActionType.ShareInfo], totalCount),
            AskHelpRate = CalculateRate(counts[AgentActionType.AskHelp], totalCount),
            ProposeIdeaRate = CalculateRate(counts[AgentActionType.ProposeIdea], totalCount),
            CriticizeRate = CalculateRate(counts[AgentActionType.Criticize], totalCount),
            WorkAloneRate = CalculateRate(counts[AgentActionType.WorkAlone], totalCount),
            SupportOtherRate = CalculateRate(counts[AgentActionType.SupportOther], totalCount),
            WaitRate = CalculateRate(counts[AgentActionType.Wait], totalCount),
            OtherRate = CalculateRate(counts[OtherAction], totalCount)
        };
    }

    private static double CalculateRate(int count, int totalCount)
    {
        return totalCount == 0 ? 0 : Math.Round(count / (double)totalCount, 4);
    }
}

public sealed class ActionDistributionSummary
{
    public int TotalCount { get; init; }
    public int ShareInfoCount { get; init; }
    public int AskHelpCount { get; init; }
    public int ProposeIdeaCount { get; init; }
    public int CriticizeCount { get; init; }
    public int WorkAloneCount { get; init; }
    public int SupportOtherCount { get; init; }
    public int WaitCount { get; init; }
    public int OtherCount { get; init; }
    public double ShareInfoRate { get; init; }
    public double AskHelpRate { get; init; }
    public double ProposeIdeaRate { get; init; }
    public double CriticizeRate { get; init; }
    public double WorkAloneRate { get; init; }
    public double SupportOtherRate { get; init; }
    public double WaitRate { get; init; }
    public double OtherRate { get; init; }
}
