using System.Collections.Generic;
using System.Linq;

namespace EmergentEngineering.Services;

public sealed record ProductivityIndicators
{
    public double? PhaseStability { get; init; }
    public double? PipelineCompletionScore { get; init; }
    public double? EffectiveDensity { get; init; }
    public double? ChaosRate { get; init; }
    public double? CollapseRate { get; init; }
    public double? SiloRate { get; init; }
    public double? KnowledgeReconfigurationScore { get; init; }
    public double? KnowledgeRecombinationScore { get; init; }
    public double? SerendipityScore { get; init; }
    public double? ExplorationScore { get; init; }
    public double? CrossDomainExposure { get; init; }
    public double? ChallengeAcceptanceScore { get; init; }
    public double? EmergentRate { get; init; }
    public double? LearningScore { get; init; }
    public double? AdaptationScore { get; init; }
    public double? AverageTrust { get; init; }
    public double? AverageRespect { get; init; }
    public double? AverageIntellectualRespect { get; init; }
    public double? MutualMentorshipScore { get; init; }
}

public sealed record ProductivityEvaluation(
    double ManagementProductivityScore,
    double EmergenceProductivityScore,
    double InstitutionalizationProductivityScore,
    double CompositeProductivityScore,
    string Interpretation);

public sealed class ProductivityEvaluationService
{
    public ProductivityEvaluation Evaluate(ProductivityIndicators indicators)
    {
        var managementScore = Average(
            indicators.PhaseStability,
            indicators.PipelineCompletionScore,
            indicators.EffectiveDensity,
            Invert(indicators.ChaosRate),
            Invert(indicators.CollapseRate),
            Invert(indicators.SiloRate));

        var emergenceScore = Average(
            indicators.KnowledgeReconfigurationScore,
            indicators.KnowledgeRecombinationScore,
            indicators.SerendipityScore,
            indicators.ExplorationScore,
            indicators.CrossDomainExposure,
            indicators.ChallengeAcceptanceScore,
            indicators.EmergentRate);

        var institutionalizationScore = Average(
            indicators.LearningScore,
            indicators.AdaptationScore,
            indicators.PipelineCompletionScore,
            indicators.AverageTrust,
            indicators.EffectiveDensity,
            indicators.AverageRespect,
            indicators.AverageIntellectualRespect,
            indicators.MutualMentorshipScore);

        return CreateEvaluation(managementScore, emergenceScore, institutionalizationScore);
    }

    public ProductivityEvaluation Average(IEnumerable<ProductivityEvaluation> evaluations)
    {
        var list = evaluations.ToList();
        if (list.Count == 0)
        {
            return new ProductivityEvaluation(0, 0, 0, 0, "まだ生産性評価を表示できるデータはありません。");
        }

        var management = Clamp01(list.Average(item => item.ManagementProductivityScore));
        var emergence = Clamp01(list.Average(item => item.EmergenceProductivityScore));
        var institutionalization = Clamp01(list.Average(item => item.InstitutionalizationProductivityScore));
        return CreateEvaluation(management, emergence, institutionalization);
    }

    public ProductivityEvaluation CreateEvaluation(
        double managementProductivityScore,
        double emergenceProductivityScore,
        double institutionalizationProductivityScore)
    {
        managementProductivityScore = Clamp01(managementProductivityScore);
        emergenceProductivityScore = Clamp01(emergenceProductivityScore);
        institutionalizationProductivityScore = Clamp01(institutionalizationProductivityScore);

        var composite = Math.Round(
            (managementProductivityScore + emergenceProductivityScore + institutionalizationProductivityScore) / 3.0,
            3);

        return new ProductivityEvaluation(
            ManagementProductivityScore: Math.Round(managementProductivityScore, 3),
            EmergenceProductivityScore: Math.Round(emergenceProductivityScore, 3),
            InstitutionalizationProductivityScore: Math.Round(institutionalizationProductivityScore, 3),
            CompositeProductivityScore: composite,
            Interpretation: BuildInterpretation(
                managementProductivityScore,
                emergenceProductivityScore,
                institutionalizationProductivityScore));
    }

    private static double Average(params double?[] values)
    {
        var validValues = values
            .Select(Normalize)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        if (validValues.Count == 0)
        {
            return 0;
        }

        return Clamp01(Math.Round(validValues.Average(), 3));
    }

    private static double? Normalize(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
        {
            return null;
        }

        return Clamp01(value.Value);
    }

    private static double? Invert(double? value)
    {
        var normalized = Normalize(value);
        return normalized.HasValue ? 1 - normalized.Value : null;
    }

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static string BuildInterpretation(
        double managementProductivityScore,
        double emergenceProductivityScore,
        double institutionalizationProductivityScore)
    {
        if (managementProductivityScore >= 0.65 && emergenceProductivityScore < 0.45)
        {
            return "管理生産性は高い一方で創発生産性が低く、既存業務の安定運用に偏っています。";
        }

        if (emergenceProductivityScore >= 0.65 && institutionalizationProductivityScore < 0.45)
        {
            return "創発生産性は高いものの、定着生産性が低く、新しい価値を組織能力へ変換する段階に課題があります。";
        }

        if (emergenceProductivityScore >= 0.60 && institutionalizationProductivityScore >= 0.60)
        {
            return "創発生産性と定着生産性がともに高く、継続的に新しい価値を生み出す状態です。";
        }

        if (managementProductivityScore < 0.45 && emergenceProductivityScore >= 0.60)
        {
            return "創造性はありますが、安定運用・再現性に課題があります。";
        }

        if (managementProductivityScore < 0.45 && emergenceProductivityScore < 0.45)
        {
            return "管理生産性と創発生産性の両方が低く、組織の回転が弱い状態です。";
        }

        return "管理・創発・定着のバランスは中程度です。";
    }
}
