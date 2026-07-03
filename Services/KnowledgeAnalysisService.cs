using System.Text.Json;
using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class KnowledgeAnalysisService
{
    public static double CalculateKnowledgeStockDelta(ActionDistributionSummary actions)
    {
        return Math.Round(
            (actions.ShareInfoRate * 0.03)
            + (actions.SupportOtherRate * 0.02)
            + (actions.AskHelpRate * 0.01)
            - (actions.WorkAloneRate * 0.01),
            4);
    }

    public static double CalculateKnowledgeDiversityDelta(
        ActionDistributionSummary actions,
        double psychologicalSafetyLevel,
        double crossDomainExposure,
        double externalShockLevel,
        double rewiringSensitivity)
    {
        var rawDelta =
            (actions.ProposeIdeaRate * 0.03)
            + (actions.CriticizeRate * psychologicalSafetyLevel * 0.02)
            + (crossDomainExposure * 0.01)
            + (externalShockLevel * 0.01);

        var sensitivityScale = 0.5 + (Math.Clamp(rewiringSensitivity, 0, 1) * 0.5);
        return Math.Round(rawDelta * sensitivityScale, 4);
    }

    public static double CalculateKnowledgeRewiringScore(
        double knowledgeDiversity,
        double crossDomainExposure,
        double externalShockLevel,
        ActionDistributionSummary actions,
        double psychologicalSafetyLevel,
        double rewiringSensitivity)
    {
        var rawScore =
            (knowledgeDiversity * 0.30)
            + (crossDomainExposure * 0.25)
            + (externalShockLevel * 0.20)
            + (actions.ProposeIdeaRate * 0.15)
            + (actions.CriticizeRate * psychologicalSafetyLevel * 0.10);

        var sensitivityScale = 0.5 + (Math.Clamp(rewiringSensitivity, 0, 1) * 0.5);
        return Clamp01(Math.Round(rawScore * sensitivityScale, 4));
    }

    public static string BuildInterpretation(
        double knowledgeStock,
        double knowledgeDiversity,
        double externalShockLevel,
        double crossDomainExposure,
        double rewiringScore,
        double workAloneRate)
    {
        if (externalShockLevel >= 0.25)
        {
            return "External shock is disturbing the knowledge network.";
        }

        if (crossDomainExposure >= 0.30 && rewiringScore >= 0.45)
        {
            return "Cross-domain exposure and idea generation are increasing rewiring potential.";
        }

        if (workAloneRate >= 0.35 && rewiringScore <= 0.30)
        {
            return "Solo work is dominant, so knowledge is not becoming organizational.";
        }

        if (knowledgeStock >= 0.40 && rewiringScore <= 0.40)
        {
            return "Knowledge stock is accumulating and learning is stabilizing.";
        }

        if (knowledgeDiversity >= 0.45 || rewiringScore >= 0.50)
        {
            return "New knowledge combinations are emerging and rewiring is active.";
        }

        return "Knowledge stock and diversity are increasing gradually.";
    }

    public static List<KnowledgeTimelinePoint> BuildTimeline(IEnumerable<SimulationStep> steps)
    {
        return steps
            .OrderBy(step => step.StepNo)
            .Select(ParseState)
            .Where(point => point is not null)
            .Cast<KnowledgeTimelinePoint>()
            .ToList();
    }

    private static KnowledgeTimelinePoint? ParseState(SimulationStep step)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(step.StateJson) ? "{}" : step.StateJson);
            var root = document.RootElement;

            return new KnowledgeTimelinePoint
            {
                StepNo = step.StepNo,
                KnowledgeStock = GetDouble(root, "knowledgeStock", KnowledgeDefaults.Stock),
                KnowledgeDiversity = GetDouble(root, "knowledgeDiversity", KnowledgeDefaults.Diversity),
                ExternalShockLevel = GetDouble(root, "externalShockLevel", KnowledgeDefaults.ExternalShockLevel),
                CrossDomainExposure = GetDouble(root, "crossDomainExposure", KnowledgeDefaults.CrossDomainExposure),
                KnowledgeRewiringScore = GetDouble(root, "knowledgeRewiringScore", 0),
                ShockOccurred = GetBool(root, "shockOccurred"),
                ShockType = GetString(root, "shockType", ShockTypes.None),
                Interpretation = GetString(root, "knowledgeInterpretation", "")
            };
        }
        catch
        {
            return new KnowledgeTimelinePoint
            {
                StepNo = step.StepNo,
                KnowledgeStock = KnowledgeDefaults.Stock,
                KnowledgeDiversity = KnowledgeDefaults.Diversity,
                ExternalShockLevel = KnowledgeDefaults.ExternalShockLevel,
                CrossDomainExposure = KnowledgeDefaults.CrossDomainExposure,
                KnowledgeRewiringScore = 0,
                ShockOccurred = false,
                ShockType = ShockTypes.None,
                Interpretation = ""
            };
        }
    }

    private static double GetDouble(JsonElement root, string propertyName, double fallback)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return fallback;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value)
            ? Math.Clamp(value, 0, 1)
            : fallback;
    }

    private static bool GetBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            && property.GetBoolean();
    }

    private static string GetString(JsonElement root, string propertyName, string fallback)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;
    }

    private static double Clamp01(double value)
    {
        return Math.Clamp(value, 0, 1);
    }
}

public sealed class KnowledgeTimelinePoint
{
    public int StepNo { get; init; }
    public double KnowledgeStock { get; init; }
    public double KnowledgeDiversity { get; init; }
    public double ExternalShockLevel { get; init; }
    public double CrossDomainExposure { get; init; }
    public double KnowledgeRewiringScore { get; init; }
    public bool ShockOccurred { get; init; }
    public string ShockType { get; init; } = ShockTypes.None;
    public string Interpretation { get; init; } = "";
}
