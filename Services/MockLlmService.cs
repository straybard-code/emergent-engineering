using System.Text.Json;
using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public sealed class MockLlmService : ILlmService
{
    private static readonly Dictionary<string, string[]> BaseMessagesByAction = new()
    {
        [AgentActionType.ShareInfo] =
        [
            "I am sharing the latest context so the team can coordinate around the next move.",
            "Here is the information I gathered so everyone can make better decisions.",
            "I want to surface what I learned before the team commits further effort."
        ],
        [AgentActionType.AskHelp] =
        [
            "I need support on a blocker and want another perspective before acting further.",
            "I am asking for help so we can reduce uncertainty and move with less risk.",
            "I want input from the team before I lock into one path."
        ],
        [AgentActionType.ProposeIdea] =
        [
            "I have a concrete proposal that could improve the next step.",
            "I see a focused experiment we can run to move the KPI.",
            "I want to test a new idea that may unlock better progress."
        ],
        [AgentActionType.Criticize] =
        [
            "I want to challenge the current approach constructively so we can improve it.",
            "I see a weakness in the plan and want to surface it before it grows.",
            "I am raising a concern because decision quality matters here."
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
            "I will pause briefly and observe before choosing the next move.",
            "I do not have enough clarity yet, so I am waiting and watching.",
            "I am holding action until the signal becomes clearer."
        ]
    };

    private static readonly Dictionary<string, string[]> BaseMemoriesByAction = new()
    {
        [AgentActionType.ShareInfo] =
        [
            "Updated memory: information sharing improved collective visibility.",
            "Updated memory: sharing context helped alignment around the KPI.",
            "Updated memory: explicit knowledge transfer reduced blind spots."
        ],
        [AgentActionType.AskHelp] =
        [
            "Updated memory: asking for help reduced uncertainty quickly.",
            "Updated memory: support requests work when the blocker is stated clearly.",
            "Updated memory: help-seeking improved learning speed."
        ],
        [AgentActionType.ProposeIdea] =
        [
            "Updated memory: proposals create movement when tied to evidence.",
            "Updated memory: ideas gain traction when they connect to the KPI.",
            "Updated memory: experimentation opens better options when the team stays responsive."
        ],
        [AgentActionType.Criticize] =
        [
            "Updated memory: constructive criticism can improve quality without stopping progress.",
            "Updated memory: tension is useful when it stays anchored to the mission.",
            "Updated memory: critique needs care so the team remains engaged."
        ],
        [AgentActionType.WorkAlone] =
        [
            "Updated memory: solo execution is efficient when speed matters most.",
            "Updated memory: independent work can produce output but risks local optimization.",
            "Updated memory: individual focus helps delivery when coordination costs are high."
        ],
        [AgentActionType.SupportOther] =
        [
            "Updated memory: supporting others increased trust and shared momentum.",
            "Updated memory: reinforcement of another agent improved team coherence.",
            "Updated memory: mutual support makes the organization more resilient."
        ],
        [AgentActionType.Wait] =
        [
            "Updated memory: waiting can preserve energy, but too much waiting slows emergence.",
            "Updated memory: uncertainty remained high, so observation felt safer than action.",
            "Updated memory: inactivity tends to spread when the team lacks direction."
        ]
    };

    private static readonly string[] CollaborationKeywords = ["\u60c5\u5831\u5171\u6709", "\u5171\u6709", "\u900f\u660e\u6027"];
    private static readonly string[] CompetitionKeywords = ["\u6210\u679c\u4e3b\u7fa9", "\u500b\u4eba\u6210\u679c", "\u7af6\u4e89"];
    private static readonly string[] CooperationKeywords = ["\u5229\u4ed6", "\u5354\u8abf", "\u52a9\u3051\u5408\u3044"];
    private static readonly string[] SafetyKeywords = ["\u5931\u6557\u3092\u8a31\u5bb9", "\u5fc3\u7406\u7684\u5b89\u5168\u6027"];
    private static readonly string[] ShortTermKpiKeywords = ["\u58f2\u4e0a", "\u63a1\u7528\u7387", "\u77ed\u671f\u6210\u679c"];
    private static readonly string[] LearningKpiKeywords = ["\u5e02\u5834\u5b66\u7fd2", "\u54c1\u8cea", "\u30ea\u30d4\u30fc\u30c8", "\u9867\u5ba2\u6210\u529f"];

    public Task<LlmAgentResponse> CompleteAgentTurnAsync(
        string prompt,
        string modelName,
        CancellationToken cancellationToken = default)
    {
        var context = ParsePrompt(prompt);
        var weights = InitializeWeights();

        ApplyWeights(weights, context.BoundaryConditions, CollaborationKeywords, AgentActionType.ShareInfo, 4, AgentActionType.AskHelp, 3, AgentActionType.SupportOther, 3);
        ApplyWeights(weights, context.BoundaryConditions, CompetitionKeywords, AgentActionType.WorkAlone, 4, AgentActionType.ProposeIdea, 3, AgentActionType.SupportOther, -2);
        ApplyWeights(weights, context.BoundaryConditions, CooperationKeywords, AgentActionType.SupportOther, 4, AgentActionType.AskHelp, 3);
        ApplyWeights(weights, context.BoundaryConditions, SafetyKeywords, AgentActionType.Criticize, 2, AgentActionType.ProposeIdea, 3);
        ApplyWeights(weights, context.KpiDefinition, ShortTermKpiKeywords, AgentActionType.WorkAlone, 3, AgentActionType.ProposeIdea, 3);
        ApplyWeights(weights, context.KpiDefinition, LearningKpiKeywords, AgentActionType.ShareInfo, 3, AgentActionType.AskHelp, 3, AgentActionType.SupportOther, 3, AgentActionType.Criticize, 2);
        ApplyParameterWeights(weights, context);

        ApplyPersonalityWeights(weights, context.Personality);
        ApplyOrientationWeights(weights, context.Orientation);
        ApplyChallengeWeights(weights, context);

        var action = WeightedChoice(weights);
        var target = ResolveTargetAgentName(action, context);
        var message = BuildMessage(action, context, target);
        var memory = BuildMemory(action, context);

        return Task.FromResult(new LlmAgentResponse
        {
            Message = message,
            Memory = memory,
            Action = action,
            TargetAgentName = target,
            RawResponse = $$"""
            {"provider":"Mock","model":"{{EscapeForJson(string.IsNullOrWhiteSpace(modelName) ? LlmDefaults.MockModel : modelName)}}","message":"{{EscapeForJson(message)}}","memory":"{{EscapeForJson(memory)}}","action":"{{action}}","targetAgentName":"{{EscapeForJson(target)}}"}
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

        for (var index = 0; index < adjustments.Length; index += 2)
        {
            var action = (string)adjustments[index];
            var delta = (int)adjustments[index + 1];
            weights[action] = Math.Max(0, weights[action] + delta);
        }
    }

    private static void ApplyPersonalityWeights(Dictionary<string, int> weights, string personality)
    {
        switch (personality)
        {
            case "Conservative":
                Adjust(weights, AgentActionType.WorkAlone, 3, AgentActionType.Wait, 2, AgentActionType.ProposeIdea, -1);
                break;
            case "Challenger":
                Adjust(weights, AgentActionType.ProposeIdea, 4, AgentActionType.Criticize, 2);
                break;
            case "Coordinator":
                Adjust(weights, AgentActionType.ShareInfo, 4, AgentActionType.AskHelp, 2);
                break;
            case "Critic":
                Adjust(weights, AgentActionType.Criticize, 4, AgentActionType.ShareInfo, 1);
                break;
            case "Supporter":
                Adjust(weights, AgentActionType.SupportOther, 4, AgentActionType.AskHelp, 2);
                break;
            case "Analyst":
                Adjust(weights, AgentActionType.ShareInfo, 3, AgentActionType.Criticize, 2);
                break;
        }
    }

    private static void ApplyOrientationWeights(Dictionary<string, int> weights, string orientation)
    {
        switch (orientation)
        {
            case "CustomerFocused":
                Adjust(weights, AgentActionType.ShareInfo, 2, AgentActionType.ProposeIdea, 2);
                break;
            case "QualityFocused":
                Adjust(weights, AgentActionType.Criticize, 2, AgentActionType.ShareInfo, 2);
                break;
            case "FieldFocused":
                Adjust(weights, AgentActionType.Criticize, 2, AgentActionType.SupportOther, 2);
                break;
            case "LearningFocused":
                Adjust(weights, AgentActionType.ShareInfo, 2, AgentActionType.AskHelp, 2, AgentActionType.SupportOther, 2);
                break;
            case "SpeedFocused":
                Adjust(weights, AgentActionType.ProposeIdea, 2, AgentActionType.WorkAlone, 2);
                break;
            case "CostFocused":
                Adjust(weights, AgentActionType.Criticize, 2, AgentActionType.WorkAlone, 2);
                break;
        }
    }

    private static void Adjust(Dictionary<string, int> weights, params object[] adjustments)
    {
        for (var index = 0; index < adjustments.Length; index += 2)
        {
            var action = (string)adjustments[index];
            var delta = (int)adjustments[index + 1];
            weights[action] = Math.Max(0, weights[action] + delta);
        }
    }

    private static bool ContainsAny(string source, IReadOnlyCollection<string> keywords)
    {
        return keywords.Any(keyword => source.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplyParameterWeights(Dictionary<string, int> weights, PromptContext context)
    {
        AdjustByLevel(weights, context.InformationSharingLevel, AgentActionType.ShareInfo, 4);
        AdjustByLevel(weights, context.InformationSharingLevel, AgentActionType.AskHelp, 1);
        AdjustByLevel(weights, context.CooperationLevel, AgentActionType.SupportOther, 4);
        AdjustByLevel(weights, context.CooperationLevel, AgentActionType.AskHelp, 2);
        AdjustByLevel(weights, context.CompetitionLevel, AgentActionType.WorkAlone, 4);
        AdjustByLevel(weights, context.CompetitionLevel, AgentActionType.ProposeIdea, 2);
        if (context.PsychologicalSafetyLevel >= 0.7)
        {
            Adjust(weights,
                AgentActionType.ShareInfo, 3,
                AgentActionType.AskHelp, 3,
                AgentActionType.ProposeIdea, 3,
                AgentActionType.SupportOther, 2,
                AgentActionType.Criticize, 1);
        }
        else if (context.PsychologicalSafetyLevel <= 0.3)
        {
            Adjust(weights,
                AgentActionType.WorkAlone, 3,
                AgentActionType.Wait, 2,
                AgentActionType.ProposeIdea, -2,
                AgentActionType.AskHelp, -2,
                AgentActionType.SupportOther, -1,
                AgentActionType.Criticize, 2);
        }
        else
        {
            AdjustByLevel(weights, context.PsychologicalSafetyLevel, AgentActionType.ShareInfo, 2);
            AdjustByLevel(weights, context.PsychologicalSafetyLevel, AgentActionType.AskHelp, 2);
            AdjustByLevel(weights, context.PsychologicalSafetyLevel, AgentActionType.ProposeIdea, 2);
            AdjustByLevel(weights, context.PsychologicalSafetyLevel, AgentActionType.SupportOther, 1);
            AdjustByLevel(weights, context.PsychologicalSafetyLevel, AgentActionType.Criticize, 1);
        }
        AdjustByLevel(weights, context.LearningOrientationLevel, AgentActionType.ShareInfo, 2);
        AdjustByLevel(weights, context.LearningOrientationLevel, AgentActionType.Criticize, 2);
        AdjustByLevel(weights, context.LearningOrientationLevel, AgentActionType.ProposeIdea, 2);
        AdjustByLevel(weights, context.CustomerOrientationLevel, AgentActionType.ShareInfo, 2);
        AdjustByLevel(weights, context.CustomerOrientationLevel, AgentActionType.ProposeIdea, 2);
        AdjustByLevel(weights, context.ShortTermResultPressureLevel, AgentActionType.WorkAlone, 3);
        AdjustByLevel(weights, context.ShortTermResultPressureLevel, AgentActionType.ProposeIdea, 3);
        AdjustByLevel(weights, context.ShortTermResultPressureLevel, AgentActionType.AskHelp, -2);
        AdjustByLevel(weights, context.KnowledgeStock, AgentActionType.ShareInfo, 2);
        AdjustByLevel(weights, context.KnowledgeStock, AgentActionType.SupportOther, 1);
        AdjustByLevel(weights, context.KnowledgeDiversity, AgentActionType.ProposeIdea, 3);
        AdjustByLevel(weights, context.KnowledgeDiversity, AgentActionType.ShareInfo, 1);
        AdjustByLevel(weights, context.ExternalShockLevel, AgentActionType.ProposeIdea, 2);
        AdjustByLevel(weights, context.ExternalShockLevel, AgentActionType.Criticize, 1);
        AdjustByLevel(weights, context.ExternalShockLevel, AgentActionType.AskHelp, 1);
        AdjustByLevel(weights, context.CrossDomainExposure, AgentActionType.ProposeIdea, 3);
        AdjustByLevel(weights, context.CrossDomainExposure, AgentActionType.ShareInfo, 2);
        AdjustByLevel(weights, context.CrossDomainExposure, AgentActionType.Criticize, 1);
        AdjustByLevel(weights, context.RewiringSensitivity, AgentActionType.ProposeIdea, 2);
        AdjustByLevel(weights, context.RewiringSensitivity, AgentActionType.Criticize, 1);
    }

    private static void ApplyChallengeWeights(Dictionary<string, int> weights, PromptContext context)
    {
        if (!context.ChallengeActive && context.ChallengeLevel <= 0)
        {
            return;
        }

        AdjustByLevel(weights, context.ChallengeLevel, AgentActionType.ShareInfo, 3);
        AdjustByLevel(weights, context.ChallengeLevel, AgentActionType.AskHelp, 3);
        AdjustByLevel(weights, context.ChallengeLevel, AgentActionType.SupportOther, 3);
        AdjustByLevel(weights, context.ChallengeLevel, AgentActionType.ProposeIdea, 3);
        AdjustByLevel(weights, context.ChallengeLevel, AgentActionType.WorkAlone, -3);

        if (context.PsychologicalSafetyLevel >= 0.5)
        {
            AdjustByLevel(weights, context.ChallengeLevel, AgentActionType.Criticize, 2);
        }
        else
        {
            AdjustByLevel(weights, context.ChallengeLevel, AgentActionType.Criticize, 1);
            AdjustByLevel(weights, context.ChallengeLevel, AgentActionType.WorkAlone, 1);
        }
    }

    private static void AdjustByLevel(Dictionary<string, int> weights, double level, string action, int magnitude)
    {
        var delta = (int)Math.Round((level - 0.5) * 2 * magnitude);
        weights[action] = Math.Max(0, weights[action] + delta);
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

    private static string BuildMessage(string action, PromptContext context, string target)
    {
        var baseMessage = Pick(BaseMessagesByAction[action]);
        var personalitySentence = GetPersonalitySentence(context.Personality, action, target);
        var orientationSentence = GetOrientationSentence(context.Orientation);
        return JoinSentences(baseMessage, personalitySentence, orientationSentence);
    }

    private static string BuildMemory(string action, PromptContext context)
    {
        var baseMemory = Pick(BaseMemoriesByAction[action]);
        var diversityMemory = JoinSentences(GetPersonalityMemory(context.Personality), GetOrientationMemory(context.Orientation));
        return JoinSentences(baseMemory, diversityMemory);
    }

    private static string GetPersonalitySentence(string personality, string action, string target)
    {
        return personality switch
        {
            "Conservative" => "I prefer a safe and proven move before taking on additional risk.",
            "Challenger" => "I want to test assumptions directly instead of preserving the current default.",
            "Coordinator" => string.IsNullOrWhiteSpace(target)
                ? "I am trying to align the team before effort fragments."
                : $"I want to align with {target} so the team stays coordinated.",
            "Critic" when action == AgentActionType.Criticize => "I am framing this as a constructive critique so the plan gets stronger.",
            "Critic" => "I am watching for contradictions and weak assumptions.",
            "Supporter" => string.IsNullOrWhiteSpace(target)
                ? "I want the next move to preserve trust across the group."
                : $"I want {target} to stay supported so progress compounds.",
            "Analyst" => "I want the decision to stay tied to evidence and a clear signal.",
            _ => ""
        };
    }

    private static string GetOrientationSentence(string orientation)
    {
        return orientation switch
        {
            "CustomerFocused" => "I am weighing customer response, market signal, repeat use, and customer success.",
            "QualityFocused" => "I am checking quality, reproducibility, and reliability before we scale this move.",
            "FieldFocused" => "I am looking at practical constraints and operational fit in the field.",
            "LearningFocused" => "I want this step to improve our feedback loop and organizational learning.",
            "SpeedFocused" => "I am optimizing for fast decision speed and quick execution.",
            "CostFocused" => "I am checking cost, efficiency, and resource constraints before we commit.",
            _ => ""
        };
    }

    private static string GetPersonalityMemory(string personality)
    {
        return personality switch
        {
            "Conservative" => "Updated memory: safe execution feels preferable when the path is ambiguous.",
            "Challenger" => "Updated memory: challenging assumptions can surface hidden opportunities.",
            "Coordinator" => "Updated memory: alignment work reduces fragmentation between agents.",
            "Critic" => "Updated memory: constructive criticism is most useful when it remains specific.",
            "Supporter" => "Updated memory: support behavior often preserves momentum and trust.",
            "Analyst" => "Updated memory: evidence-backed reasoning improves follow-through.",
            _ => ""
        };
    }

    private static string GetOrientationMemory(string orientation)
    {
        return orientation switch
        {
            "CustomerFocused" => "Updated memory: market response and customer success should stay visible in the loop.",
            "QualityFocused" => "Updated memory: quality and reliability constraints shape sustainable progress.",
            "FieldFocused" => "Updated memory: operational reality narrows what is practical to execute.",
            "LearningFocused" => "Updated memory: feedback loops compound organizational learning over time.",
            "SpeedFocused" => "Updated memory: decision latency can erase otherwise good ideas.",
            "CostFocused" => "Updated memory: efficiency matters when resources are limited.",
            _ => ""
        };
    }

    private static string JoinSentences(params string[] parts)
    {
        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part.Trim()));
    }

    private static string Pick(string[] items)
    {
        return items[Random.Shared.Next(items.Length)];
    }

    private static PromptContext ParsePrompt(string prompt)
    {
        var boundaryConditions = ExtractSection(prompt, "Culture and boundary conditions:", "KPI:");
        var kpi = ExtractSection(prompt, "KPI:", "You are:");
        var selfName = ExtractLine(prompt, "Name:");
        var personality = ExtractLine(prompt, "Your personality:");
        var orientation = ExtractLine(prompt, "Your orientation:");
        var informationSharingLevel = ExtractNumericLine(prompt, "InformationSharingLevel:");
        var cooperationLevel = ExtractNumericLine(prompt, "CooperationLevel:");
        var competitionLevel = ExtractNumericLine(prompt, "CompetitionLevel:");
        var psychologicalSafetyLevel = ExtractNumericLine(prompt, "PsychologicalSafetyLevel:");
        var learningOrientationLevel = ExtractNumericLine(prompt, "LearningOrientationLevel:");
        var customerOrientationLevel = ExtractNumericLine(prompt, "CustomerOrientationLevel:");
        var shortTermResultPressureLevel = ExtractNumericLine(prompt, "ShortTermResultPressureLevel:");
        var knowledgeStock = ExtractNumericLine(prompt, "KnowledgeStock:");
        var knowledgeDiversity = ExtractNumericLine(prompt, "KnowledgeDiversity:");
        var externalShockLevel = ExtractNumericLine(prompt, "ExternalShockLevel:");
        var crossDomainExposure = ExtractNumericLine(prompt, "CrossDomainExposure:");
        var rewiringSensitivity = ExtractNumericLine(prompt, "RewiringSensitivity:");
        var challengeLevel = ExtractNumericLine(prompt, "ChallengeLevel:");
        var challengeActive = ExtractBoolLine(prompt, "ChallengeActive:");
        var otherAgents = ExtractSection(prompt, "Other agents:", "Recent messages from the previous step:");

        var names = otherAgents
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(name => !name.Equals("None", StringComparison.OrdinalIgnoreCase))
            .Where(name => !name.Equals(selfName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PromptContext(
            boundaryConditions,
            kpi,
            personality,
            orientation,
            informationSharingLevel,
            cooperationLevel,
            competitionLevel,
            psychologicalSafetyLevel,
            learningOrientationLevel,
            customerOrientationLevel,
            shortTermResultPressureLevel,
            knowledgeStock,
            knowledgeDiversity,
            externalShockLevel,
            crossDomainExposure,
            rewiringSensitivity,
            challengeLevel,
            challengeActive,
            names);
    }

    private static string ExtractSection(string prompt, string startMarker, string endMarker)
    {
        var start = prompt.IndexOf(startMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            return "";
        }

        start += startMarker.Length;
        var end = prompt.IndexOf(endMarker, start, StringComparison.Ordinal);
        if (end < 0)
        {
            end = prompt.Length;
        }

        return prompt[start..end].Trim();
    }

    private static string ExtractLine(string prompt, string marker)
    {
        var start = prompt.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return "";
        }

        start += marker.Length;
        var end = prompt.IndexOf('\n', start);
        if (end < 0)
        {
            end = prompt.Length;
        }

        return prompt[start..end].Trim();
    }

    private static double ExtractNumericLine(string prompt, string marker)
    {
        var value = ExtractLine(prompt, marker);
        return double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? Math.Clamp(parsed, 0, 1)
            : BoundaryParameterDefaults.Level;
    }

    private static bool ExtractBoolLine(string prompt, string marker)
    {
        var value = ExtractLine(prompt, marker);
        return bool.TryParse(value, out var parsed) && parsed;
    }

    private static string EscapeForJson(string value)
    {
        return JsonSerializer.Serialize(value).Trim('"');
    }

    private sealed record PromptContext(
        string BoundaryConditions,
        string KpiDefinition,
        string Personality,
        string Orientation,
        double InformationSharingLevel,
        double CooperationLevel,
        double CompetitionLevel,
        double PsychologicalSafetyLevel,
        double LearningOrientationLevel,
        double CustomerOrientationLevel,
        double ShortTermResultPressureLevel,
        double KnowledgeStock,
        double KnowledgeDiversity,
        double ExternalShockLevel,
        double CrossDomainExposure,
        double RewiringSensitivity,
        double ChallengeLevel,
        bool ChallengeActive,
        List<string> AgentNames);
}
