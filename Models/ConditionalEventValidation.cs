using System.ComponentModel.DataAnnotations;

namespace EmergentEngineering.Models;

public static class ConditionalEventValidation
{
    public static IEnumerable<ValidationResult> ValidateExternalShock(
        bool enableExternalShock,
        string? shockType,
        int shockStep,
        double externalShockLevel)
    {
        if (!enableExternalShock)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(shockType) || string.Equals(shockType.Trim(), ShockTypes.None, StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("外部刺激タイプを選択してください。", ["ShockType"]);
        }

        if (shockStep == 0)
        {
            yield return new ValidationResult("外部刺激Stepは 1 以上を指定してください。", ["ShockStep"]);
        }

        if (externalShockLevel == 0)
        {
            yield return new ValidationResult("外部刺激レベルは 0 より大きい値を指定してください。", ["ExternalShockLevel"]);
        }
    }

    public static IEnumerable<ValidationResult> ValidateChallenge(
        bool enableChallengeEvent,
        string? challengeType,
        int challengeStep,
        double challengeLevel,
        double requiredKnowledgeDiversity,
        double requiredCrossDomainExposure,
        double requiredRewiringScore)
    {
        if (!enableChallengeEvent)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(challengeType) || string.Equals(challengeType.Trim(), ChallengeTypes.None, StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Challengeタイプを選択してください。", ["ChallengeType"]);
        }

        if (challengeStep == 0)
        {
            yield return new ValidationResult("Challenge発生Stepは 1 以上を指定してください。", ["ChallengeStep"]);
        }

        if (challengeLevel == 0)
        {
            yield return new ValidationResult("Challengeレベルは 0 より大きい値を指定してください。", ["ChallengeLevel"]);
        }

        if (requiredKnowledgeDiversity == 0)
        {
            yield return new ValidationResult("必要知識多様性は 0 より大きい値を指定してください。", ["RequiredKnowledgeDiversity"]);
        }

        if (requiredCrossDomainExposure == 0)
        {
            yield return new ValidationResult("必要異分野接触度は 0 より大きい値を指定してください。", ["RequiredCrossDomainExposure"]);
        }

        if (requiredRewiringScore == 0)
        {
            yield return new ValidationResult("必要再配線スコアは 0 より大きい値を指定してください。", ["RequiredRewiringScore"]);
        }
    }
}
