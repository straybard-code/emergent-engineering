using System.Globalization;
using System.Text.Json;

namespace EmergentEngineering.Models;

public static class TrustJsonUtility
{
    public static Dictionary<string, double> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            }

            var trustMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (TryReadTrustValue(property.Value, out var value))
                {
                    trustMap[property.Name] = Clamp(value);
                }
            }

            return trustMap;
        }
        catch
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static double CalculateAverage(string? json)
    {
        var values = Deserialize(json).Values.ToList();
        return values.Count == 0 ? 0 : Math.Round(values.Average(), 2);
    }

    public static double Clamp(double value)
    {
        return Math.Clamp(Math.Round(value, 2), -1.0, 1.0);
    }

    public static bool TryReadTrustValue(JsonElement element, out double value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.TryGetDouble(out value);

            case JsonValueKind.String:
                return double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                    || double.TryParse(element.GetString(), out value);

            default:
                value = 0;
                return false;
        }
    }
}
