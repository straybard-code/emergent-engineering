namespace EmergentEngineering.Models;

public interface IThanksCoinSettings
{
    bool EnableThanksCoin { get; set; }
    double ThanksCoinRate { get; set; }
    double ThanksCoinRespectGain { get; set; }
    double ThanksCoinTrustGain { get; set; }
    double ThanksCoinReconfigurationGain { get; set; }
    double ThanksCoinPsychologicalSafetyGain { get; set; }
    double ThanksCoinBridgeGain { get; set; }
    double ThanksCoinPopularityBias { get; set; }
    double ThanksCoinDiversityBonus { get; set; }
    double ThanksCoinChallengeBonus { get; set; }
}
