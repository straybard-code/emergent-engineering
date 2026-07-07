namespace EmergentEngineering.Models;

public interface IIntellectualRespectSettings
{
    bool EnableIntellectualRespect { get; set; }
    double IntellectualRespectBase { get; set; }
    double IntellectualRespectGrowthRate { get; set; }
    double IntellectualRespectDecayRate { get; set; }
    double IntellectualRespectDiversitySensitivity { get; set; }
    double IntellectualRespectChallengeSensitivity { get; set; }
    double IntellectualRespectMentorshipSensitivity { get; set; }
    double IntellectualRespectEgoPenalty { get; set; }
    double IntellectualRespectHierarchyPenalty { get; set; }
}
