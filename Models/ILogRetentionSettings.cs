namespace EmergentEngineering.Models;

public interface ILogRetentionSettings
{
    string LogDetailLevel { get; set; }
    int StepLogInterval { get; set; }
    int ActionLogInterval { get; set; }
}
