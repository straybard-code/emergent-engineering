using System.Text.Json;
using System.Text.RegularExpressions;
using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public sealed class MockLlmService : ILlmService
{
    private static readonly Dictionary<string, string[]> MessagesByAction = new()
    {
        [AgentActionType.ShareInfo] =
        [
            "I am sharing the latest context so the team can make better decisions.",
            "Here is the information I have gathered that may help everyone align.",
            "I want to make my findings visible before we move to the next action."
        ],
        [AgentActionType.AskHelp] =
        [
            "I need support on a blocker and would like another perspective.",
            "I am asking for help to reduce uncertainty before acting alone.",
            "I want input from the team so we can move faster with less risk."
        ],
        [AgentActionType.ProposeIdea] =
        [
            "I have a new proposal that could improve our next step.",
            "I want to test a concrete idea that may move the KPI.",
            "I see an opportunity and propose we try a focused experiment."
        ],
        [AgentActionType.Criticize] =
        [
            "I want to challenge the current approach so we can improve it.",
            "I see a weakness in the plan and want to surface it constructively.",
            "I am offering a critical view because our decision quality matters."
        ],
        [AgentActionType.WorkAlone] =
        [
            "I will work alone for this step and return with a concrete result.",
            "I am focusing on individual execution to produce short-term output.",
            "I will take ownership of a narrow task and move it forward myself."
        ],
        [AgentActionType.SupportOther] =
        [
            "I want to support another agent and reinforce the strongest progress.",
            "I can help unblock someone else so the team keeps momentum.",
            "I am choosing to back another agent's work and strengthen coordination."
        ],
        [AgentActionType.Wait] =
        [
            "I will pause for a moment and observe before choosing the next move.",
            "I do not have enough clarity yet, so I am waiting and watching.",
            "I am holding action until the team signal becomes clearer."
        ]
    };

    private static readonly Dictionary<string, string[]> MemoriesByAction = new()
    {
        [AgentActionType.ShareInfo] =
        [
            "Updated memory: information sharing improved collective visibility.",
            "Updated memory: sharing context helped alignment around the KPI.",
            "Updated memory: the team responds better when knowledge is made explicit."
        ],
        [AgentActionType.AskHelp] =
        [
            "Updated memory: asking for help reduced uncertainty quickly.",
            "Updated memory: support requests work when the blocker is stated clearly.",
            "Updated memory: help-seeking increases learning when trust is present."
        ],
        [AgentActionType.ProposeIdea] =
        [
            "Updated memory: new proposals create movement when tied to evidence.",
            "Updated memory: ideas gain traction when they connect to the KPI.",
            "Updated memory: experimentation helps the group discover better options."
        ],
        [AgentActionType.Criticize] =
        [
            "Updated memory: constructive criticism can improve quality without stopping progress.",
            "Updated memory: tension is useful when it stays anchored to the mission.",
            "Updated memory: critique needs care so the team stays engaged."
        ],
        [AgentActionType.WorkAlone] =
        [
            "Updated memory: solo execution is efficient when speed matters most.",
            "Updated memory: independent work can produce output but risks local optimization.",
            "Updated memory: individual focus helps delivery when coordination costs are high."
        ],
        [AgentActionType.SupportOther] =
        [
            "Updated memory: supporting others increases trust and shared momentum.",
            "Updated memory: reinforcement of another agent improved team coherence.",
            "Updated memory: mutual support makes the organization more resilient."
        ],
        [AgentActionType.Wait] =
        [
            "Updated memory: waiting can preserve energy, but too much waiting slows emergence.",
            "Updated memory: uncertainty remains high, so observation felt safer than action.",
            "Updated memory: inactivity tends to spread when the team lacks direction."
        ]
    };

    private static readonly string[] CollaborationKeywords = ["\u60c5\u5831\u5171\u6709", "\u5171\u6709", "\u900f\u660e\u6027"];
    private static readonly string[] CompetitionKeywords = ["\u6210\u679c\u4e3b\u7fa9", "\u500b\u4eba\u6210\u679c", "\u7af6\u4e89"];
    private static readonly string[] CooperationKeywords = ["\u5229\u4ed6", "\u5354\u8abf", "\u52a9\u3051\u5408\u3044"];
    private static readonly string[] SafetyKeywords = ["\u5931\u6557\u3092\u8a31\u5bb9", "\u5fc3\u7406\u7684\u5b89\u5168\u6027"];
    private static readonly string[] ShortTermKpiKeywords = ["\u58f2\u4e0a", "\u63a1\u7528\u7387", "\u77ed\u671f\u6210\u679c"];
    private static readonly string[] LearningKpiKeywords = ["\u5e02\u5834\u5b66\u7fd2", "\u54c1\u8cea", "\u30ea\u30d4\u30fc\u30c8", "\u9867\u5ba2\u6210\u529f"];
    private static readonly Regex SectionRegex = new(@"(?<header>Culture and boundary conditions|KPI|Name):\s*(?<content>[\s\S]*?)(?=\n[A-Z][^\n]*:\s*|\z)", RegexOptions.Compiled);

    public Task<LlmAgentResponse> CompleteAgentTurnAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var context = ParsePrompt(prompt);
        var weights = InitializeWeights();

        ApplyWeights(weights, context.BoundaryConditions, CollaborationKeywords, AgentActionType.ShareInfo, 4, AgentActionType.AskHelp, 3, AgentActionType.SupportOther, 3);
        ApplyWeights(weights, context.BoundaryConditions, CompetitionKeywords, AgentActionType.WorkAlone, 4, AgentActionType.ProposeIdea, 3, AgentActionType.SupportOther, -2);
        ApplyWeights(weights, context.BoundaryConditions, CooperationKeywords, AgentActionType.SupportOther, 4, AgentActionType.AskHelp, 3);
        ApplyWeights(weights, context.BoundaryConditions, SafetyKeywords, AgentActionType.Criticize, 2, AgentActionType.ProposeIdea, 3);
        ApplyWeights(weights, context.KpiDefinition, ShortTermKpiKeywords, AgentActionType.WorkAlone, 3, AgentActionType.ProposeIdea, 3);
        ApplyWeights(weights, context.KpiDefinition, LearningKpiKeywords, AgentActionType.ShareInfo, 3, AgentActionType.AskHelp, 3, AgentActionType.SupportOther, 3, AgentActionType.Criticize, 2);

        var action = WeightedChoice(weights);
        var target = ResolveTargetAgentName(action, context);
        var message = Pick(MessagesByAction[action]);
        var memory = Pick(MemoriesByAction[action]);

        return Task.FromResult(new LlmAgentResponse
        {
            Message = message,
            Memory = memory,
            Action = action,
            TargetAgentName = target,
            RawResponse = $$"""
            {"message":"{{EscapeForJson(message)}}","memory":"{{EscapeForJson(memory)}}","action":"{{action}}","targetAgentName":"{{EscapeForJson(target)}}"}
            """
        });
    }

    private static Dictionary<string, int> InitializeWeights()
    {
        return AgentActionType.All.ToDictionary(action => action, _ => 1);
    }

    private static void ApplyWeights(Dictionary<string, int> weights, string source, IReadOnlyCollection<string> keywords, params object[] adjustments)
    {
        if (!ContainsAny(source, keywords))
        {
            return;
        }

        for (var i = 0; i < adjustments.Length; i += 2)
        {
            var action = (string)adjustments[i];
            var delta = (int)adjustments[i + 1];
            weights[action] = Math.Max(0, weights[action] + delta);
        }
    }

    private static bool ContainsAny(string source, IReadOnlyCollection<string> keywords)
    {
        return keywords.Any(keyword => source.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static string WeightedChoice(Dictionary<string, int> weights)
    {
        var total = weights.Values.Sum();
        if (total <= 0)
        {
            return AgentActionType.Wait;
        }

        var roll = Random.Shared.Next(1, total + 1);
        var cumulative = 0;
        foreach (var pair in weights)
        {
            cumulative += pair.Value;
            if (roll <= cumulative)
            {
                return pair.Key;
            }
        }

        return AgentActionType.Wait;
    }

    private static string ResolveTargetAgentName(string action, PromptContext context)
    {
        if (action is AgentActionType.WorkAlone or AgentActionType.Wait)
        {
            return "";
        }

        if (context.AgentNames.Count == 0)
        {
            return "";
        }

        return context.AgentNames[Random.Shared.Next(context.AgentNames.Count)];
    }

    private static string Pick(string[] items)
    {
        return items[Random.Shared.Next(items.Length)];
    }

    private static PromptContext ParsePrompt(string prompt)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in SectionRegex.Matches(prompt))
        {
            sections[match.Groups["header"].Value] = match.Groups["content"].Value.Trim();
        }

        var selfName = sections.GetValueOrDefault("Name", "");
        var names = Regex.Matches(prompt, @"Agent\s+\d+", RegexOptions.IgnoreCase)
            .Select(match => match.Value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => !name.Equals(selfName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new PromptContext(
            sections.GetValueOrDefault("Culture and boundary conditions", ""),
            sections.GetValueOrDefault("KPI", ""),
            names);
    }

    private static string EscapeForJson(string value)
    {
        return JsonSerializer.Serialize(value).Trim('"');
    }

    private sealed record PromptContext(string BoundaryConditions, string KpiDefinition, List<string> AgentNames);
}
