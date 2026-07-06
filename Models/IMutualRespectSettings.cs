namespace EmergentEngineering.Models;

public interface IMutualRespectSettings
{
    double MutualRespectBase { get; set; }
    double MutualRespectGrowthRate { get; set; }
    double MutualRespectDecayRate { get; set; }
    double MutualRespectDiversitySensitivity { get; set; }
    double MutualRespectChallengeSensitivity { get; set; }
    double MutualRespectBridgeSensitivity { get; set; }
    double MutualRespectPopularityPenalty { get; set; }
}
