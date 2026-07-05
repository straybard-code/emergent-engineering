namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class ParameterSweepAnalysisPoint
{
    public double ParameterValue { get; set; }
    public double? PreviousParameterValue { get; set; }
    public double AverageTrust { get; set; }
    public double AverageAbsTrust { get; set; }
    public double EffectiveDensity { get; set; }
    public double StrongLinks { get; set; }
    public double WeakLinks { get; set; }
    public double ComponentCount { get; set; }
    public double IsolatedAgents { get; set; }
    public double AverageKnowledgeDiversity { get; set; }
    public double AverageKnowledgeRecombinationScore { get; set; }
    public double AverageKnowledgeReconfigurationScore { get; set; }
    public double AverageEmergentScore { get; set; }
    public double AverageStableScore { get; set; }
    public double AverageLearningScore { get; set; }
    public double AverageSiloScore { get; set; }
    public double AverageAdaptationScore { get; set; }
    public double AveragePipelineCompletionScore { get; set; }
    public string MostCommonPipelineBottleneck { get; set; } = "--";
    public string PipelineBottleneckInterpretation { get; set; } = "-";
    public int EmergentRunCount { get; set; }
    public int StableRunCount { get; set; }
    public int LearningRunCount { get; set; }
    public int SiloRunCount { get; set; }
    public int TotalRunCount { get; set; }
    public double EmergentRate { get; set; }
    public double StableRate { get; set; }
    public double LearningRate { get; set; }
    public double SiloRate { get; set; }
    public double AverageSerendipityScore { get; set; }
    public int SerendipityOccurredRunCount { get; set; }
    public double SerendipityRate { get; set; }
    public int SerendipityToEmergenceLinkCount { get; set; }
    public double SerendipityToEmergenceRate { get; set; }
    public double? DeltaAverageTrust { get; set; }
    public double? DeltaEffectiveDensity { get; set; }
    public double? DeltaEmergentRate { get; set; }
    public double? DeltaSerendipityRate { get; set; }
    public double? DeltaKnowledgeDiversity { get; set; }
    public double? DeltaKnowledgeRecombinationScore { get; set; }
    public double? DeltaKnowledgeReconfigurationScore { get; set; }
    public double? DeltaEmergentScore { get; set; }
    public double? DeltaStableScore { get; set; }
    public double? DeltaLearningScore { get; set; }
    public double? DeltaPipelineCompletionScore { get; set; }
    public int TrustInsufficientRunCount { get; set; }
    public int EffectiveDensityInsufficientRunCount { get; set; }
    public int StrongLinkInsufficientRunCount { get; set; }
    public int KnowledgeDiversityInsufficientRunCount { get; set; }
    public int KnowledgeRecombinationInsufficientRunCount { get; set; }
    public int KnowledgeReconfigurationInsufficientRunCount { get; set; }
    public int SerendipityInsufficientRunCount { get; set; }
    public int IdeaProposalInsufficientRunCount { get; set; }
    public int ShareInfoInsufficientRunCount { get; set; }
    public int ConstructiveCriticismInsufficientRunCount { get; set; }
    public int PsychologicalSafetyInsufficientRunCount { get; set; }
    public string MainEmergentFailureReason { get; set; } = "";
    public string TransitionCandidates { get; set; } = "";
}

public sealed class ParameterSweepSummaryCard
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "--";
}
