namespace EmergentEngineering.Models;

public interface IThinkingSpeedSettings
{
    bool EnableThinkingSpeedModel { get; set; }
    double ThinkingSpeedBase { get; set; }
    double ThinkingSpeedDispersion { get; set; }
    double OrganizationalDecisionSpeed { get; set; }
    double OrganizationalValidationSpeed { get; set; }
}
