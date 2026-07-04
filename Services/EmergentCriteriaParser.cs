using System.Text.Json;

namespace EmergentEngineering.Services;

public static class EmergentCriteriaParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["AverageTrust"] = "信頼",
        ["EffectiveDensity"] = "実効密度",
        ["StrongLinks"] = "StrongLink",
        ["KnowledgeDiversity"] = "知識多様性",
        ["KnowledgeRecombination"] = "知識再結合",
        ["KnowledgeReconfiguration"] = "知識再構成",
        ["Serendipity"] = "セレンディピティ",
        ["ProposeIdea"] = "アイデア提案",
        ["ShareInfo"] = "情報共有",
        ["ConstructiveCriticism"] = "建設的批判"
    };

    public static List<EmergentCriterionEntry> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            List<EmergentCriterionEntry> entries = [];
            foreach (var property in root.EnumerateObject())
            {
                entries.Add(ParseEntry(property.Name, property.Value));
            }

            return entries
                .OrderBy(item => item.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static Dictionary<string, bool> ToPassMap(string? json)
    {
        return Parse(json).ToDictionary(item => item.Key, item => item.Passed, StringComparer.OrdinalIgnoreCase);
    }

    private static EmergentCriterionEntry ParseEntry(string key, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
        {
            return new EmergentCriterionEntry
            {
                Key = key,
                Label = GetLabel(key),
                Passed = value.GetBoolean()
            };
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return new EmergentCriterionEntry
            {
                Key = key,
                Label = GetLabel(key),
                Value = GetNullableDouble(value, "value"),
                Threshold = GetNullableDouble(value, "threshold"),
                Passed = GetBool(value, "passed")
            };
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return new EmergentCriterionEntry
            {
                Key = key,
                Label = GetLabel(key),
                Value = value.TryGetDouble(out var numericValue) ? numericValue : null,
                Passed = value.TryGetDouble(out var boolLikeValue) && boolLikeValue > 0
            };
        }

        return new EmergentCriterionEntry
        {
            Key = key,
            Label = GetLabel(key)
        };
    }

    private static string GetLabel(string key)
    {
        return Labels.TryGetValue(key, out var label) ? label : key;
    }

    private static double? GetNullableDouble(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value)
            ? value
            : null;
    }

    private static bool GetBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property)
            && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            && property.GetBoolean();
    }
}

public sealed class EmergentCriterionEntry
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public double? Value { get; init; }
    public double? Threshold { get; init; }
    public bool Passed { get; init; }
}
