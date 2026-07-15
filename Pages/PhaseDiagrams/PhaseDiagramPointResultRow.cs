namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class PhaseDiagramPointResultRow
{
    public double XValue { get; set; }
    public double YValue { get; set; }
    public int? ExperimentId { get; set; }
    public string DominantPhase { get; set; } = "-";
    public double EmergentRate { get; set; }
    public double StableRate { get; set; }
    public double LearningRate { get; set; }
    public double SiloRate { get; set; }
    public double AverageTrust { get; set; }
    public double AverageEffectiveDensity { get; set; }
    public double AverageKnowledgeDiversity { get; set; }
    public double AverageKnowledgeReconfigurationScore { get; set; }
    public double AverageSerendipityRate { get; set; }
    public double AveragePipelineCompletionScore { get; set; }
    public bool ThinkingSpeedModelEnabled { get; set; }
    public double AverageThinkingSpeed { get; set; }
    public double MinThinkingSpeed { get; set; }
    public double MaxThinkingSpeed { get; set; }
    public double ThinkingSpeedDispersionActual { get; set; }
    public double OrganizationalDecisionSpeed { get; set; }
    public double OrganizationalValidationSpeed { get; set; }
    public double GeneratedIdeaCount { get; set; }
    public double ProcessedIdeaCount { get; set; }
    public double UnprocessedIdeaCount { get; set; }
    public double GeneratedHypothesisCount { get; set; }
    public double ValidatedHypothesisCount { get; set; }
    public double UnvalidatedHypothesisCount { get; set; }
    public double CognitiveLoad { get; set; }
    public double ThinkingSpeedMismatch { get; set; }
    public double DecisionOverloadRatio { get; set; }
    public double ValidationOverloadRatio { get; set; }
    public int ConsecutiveHighLoadSteps { get; set; }
    public double ThinkingFlowBonus { get; set; }
    public double ThinkingOverloadPenalty { get; set; }
    public double ManagementProductivityScore { get; set; }
    public double EmergenceProductivityScore { get; set; }
    public double InstitutionalizationProductivityScore { get; set; }
    public double CompositeProductivityScore { get; set; }
    public string DominantBottleneck { get; set; } = "--";
}
