using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public sealed class OpenAiLlmService(IHttpClientFactory httpClientFactory) : ILlmService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<LlmAgentResponse> CompleteAgentTurnAsync(
        string prompt,
        string modelName,
        CancellationToken cancellationToken = default)
    {
        var apiKey = Environment.GetEnvironmentVariable("ORGSIM_OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("ORGSIM_OPENAI_API_KEY is not set.");
        }

        var model = string.IsNullOrWhiteSpace(modelName) ? LlmDefaults.OpenAiRecommendedModel : modelName.Trim();

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = "You simulate one organizational agent. Return only valid JSON." },
                new { role = "user", content = prompt }
            }
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(raw);
        var json = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        var parsed = ParseAgentResponse(json);
        parsed.RawResponse = raw;
        return parsed;
    }

    private static LlmAgentResponse ParseAgentResponse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var action = root.TryGetProperty("action", out var actionElement) ? actionElement.GetString() ?? AgentActionType.Wait : AgentActionType.Wait;
        if (!AgentActionType.All.Contains(action))
        {
            action = AgentActionType.Wait;
        }

        return new LlmAgentResponse
        {
            Message = root.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "",
            Memory = root.TryGetProperty("memory", out var memory) ? memory.GetString() ?? "" : "",
            Action = action,
            TargetAgentName = root.TryGetProperty("targetAgentName", out var target) ? target.GetString() ?? "" : "",
            RawResponse = json
        };
    }
}
