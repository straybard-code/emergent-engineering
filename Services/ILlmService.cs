namespace EmergentEngineering.Services;

public interface ILlmService
{
    Task<LlmAgentResponse> CompleteAgentTurnAsync(
        string prompt,
        string modelName,
        CancellationToken cancellationToken = default);
}

public sealed class LlmAgentResponse
{
    public string Message { get; set; } = "";
    public string Memory { get; set; } = "";
    public string Action { get; set; } = "Wait";
    public string TargetAgentName { get; set; } = "";
    public string RawResponse { get; set; } = "";
}
