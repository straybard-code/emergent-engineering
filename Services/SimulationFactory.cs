using EmergentEngineering.Models;

namespace EmergentEngineering.Services;

public static class SimulationFactory
{
    private static readonly string[] Roles =
    [
        "Coordinator",
        "Analyst",
        "Builder",
        "Critic",
        "Customer Advocate",
        "Operations Lead",
        "Researcher",
        "Integrator"
    ];

    private static readonly string[] Personalities =
    [
        "Coordinator",
        "Challenger",
        "Critic",
        "Supporter",
        "Analyst",
        "Conservative"
    ];

    private static readonly string[] Orientations =
    [
        "CustomerFocused",
        "QualityFocused",
        "FieldFocused",
        "LearningFocused",
        "SpeedFocused",
        "CostFocused"
    ];

    public static Scenario CloneScenario(Scenario source)
    {
        return CreateScenario(source);
    }

    public static string BuildScenarioCopyName(string sourceName, IReadOnlyCollection<string> existingNames)
    {
        var baseName = string.IsNullOrWhiteSpace(sourceName) ? "シナリオ" : sourceName.Trim();
        var candidate = $"{baseName} - コピー";
        var names = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

        if (!names.Contains(candidate))
        {
            return candidate;
        }

        var index = 2;
        while (names.Contains($"{candidate} {index}"))
        {
            index++;
        }

        return $"{candidate} {index}";
    }

    public static SimulationProject CreateProject(CreateSimulationRequest request)
    {
        var project = new SimulationProject
        {
            Name = request.Name.Trim(),
            Purpose = request.Purpose.Trim(),
            BoundaryConditions = request.BoundaryConditions.Trim(),
            KpiDefinition = request.KpiDefinition.Trim(),
            AgentCount = request.AgentCount,
            TotalSteps = request.TotalSteps,
            LlmProvider = string.IsNullOrWhiteSpace(request.LlmProvider) ? LlmDefaults.Provider : request.LlmProvider.Trim(),
            LlmModel = string.IsNullOrWhiteSpace(request.LlmModel) ? LlmDefaults.MockModel : request.LlmModel.Trim(),
            InformationSharingLevel = request.InformationSharingLevel,
            CooperationLevel = request.CooperationLevel,
            CompetitionLevel = request.CompetitionLevel,
            PsychologicalSafetyLevel = request.PsychologicalSafetyLevel,
            LearningOrientationLevel = request.LearningOrientationLevel,
            CustomerOrientationLevel = request.CustomerOrientationLevel,
            ShortTermResultPressureLevel = request.ShortTermResultPressureLevel,
            EffectiveTrustThreshold = request.EffectiveTrustThreshold,
            KnowledgeStock = request.KnowledgeStock,
            KnowledgeDiversity = request.KnowledgeDiversity,
            ExternalShockLevel = request.ExternalShockLevel,
            CrossDomainExposure = request.CrossDomainExposure,
            RewiringSensitivity = request.RewiringSensitivity,
            EnableExternalShock = request.EnableExternalShock,
            ShockStep = request.ShockStep,
            ShockType = string.IsNullOrWhiteSpace(request.ShockType) ? ShockTypes.None : request.ShockType.Trim(),
            ShockDescription = request.ShockDescription?.Trim() ?? "",
            EnableChallengeEvent = request.EnableChallengeEvent,
            ChallengeStep = request.ChallengeStep,
            ChallengeLevel = request.ChallengeLevel,
            ChallengeType = string.IsNullOrWhiteSpace(request.ChallengeType) ? ChallengeTypes.None : request.ChallengeType.Trim(),
            ChallengeDescription = request.ChallengeDescription?.Trim() ?? "",
            RequiredKnowledgeDiversity = request.RequiredKnowledgeDiversity,
            RequiredCrossDomainExposure = request.RequiredCrossDomainExposure,
            RequiredRewiringScore = request.RequiredRewiringScore,
            ExplorationTendency = request.ExplorationTendency,
            SerendipitySensitivity = request.SerendipitySensitivity,
            KnowledgeRecombinationRate = request.KnowledgeRecombinationRate,
            SerendipityThreshold = request.SerendipityThreshold,
            EnableSerendipity = request.EnableSerendipity,
            EnableTrustDynamics = request.EnableTrustDynamics,
            TrustGrowthRate = request.TrustGrowthRate,
            TrustDecayRate = request.TrustDecayRate,
            TrustSaturationStrength = request.TrustSaturationStrength,
            TrustCapacity = request.TrustCapacity,
            TrustCapacityPenalty = request.TrustCapacityPenalty,
            DistrustPenalty = request.DistrustPenalty,
            ConstructiveCriticismBonus = request.ConstructiveCriticismBonus,
            CurrentStep = 0,
            Status = SimulationStatus.Created,
            Phase = SimulationPhase.Forming,
            CreatedAt = DateTime.UtcNow
        };

        for (var i = 1; i <= request.AgentCount; i++)
        {
            project.Agents.Add(new Agent
            {
                Name = $"Agent {i}",
                Role = Roles[(i - 1) % Roles.Length],
                Memory = "Initial memory: no shared history yet.",
                TrustJson = "{}",
                Personality = Personalities[(i - 1) % Personalities.Length],
                Orientation = Orientations[(i - 1) % Orientations.Length],
                PositionX = Math.Round(Math.Cos(i * Math.PI * 2 / request.AgentCount), 3),
                PositionY = Math.Round(Math.Sin(i * Math.PI * 2 / request.AgentCount), 3)
            });
        }

        return project;
    }

    public static Experiment CreateExperiment(CreateExperimentRequest request)
    {
        return new Experiment
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Purpose = request.Purpose.Trim(),
            BoundaryConditions = request.BoundaryConditions.Trim(),
            KpiDefinition = request.KpiDefinition.Trim(),
            AgentCount = request.AgentCount,
            TotalSteps = request.TotalSteps,
            RunCount = request.RunCount,
            LlmProvider = string.IsNullOrWhiteSpace(request.LlmProvider) ? LlmDefaults.Provider : request.LlmProvider.Trim(),
            LlmModel = string.IsNullOrWhiteSpace(request.LlmModel) ? LlmDefaults.MockModel : request.LlmModel.Trim(),
            InformationSharingLevel = request.InformationSharingLevel,
            CooperationLevel = request.CooperationLevel,
            CompetitionLevel = request.CompetitionLevel,
            PsychologicalSafetyLevel = request.PsychologicalSafetyLevel,
            LearningOrientationLevel = request.LearningOrientationLevel,
            CustomerOrientationLevel = request.CustomerOrientationLevel,
            ShortTermResultPressureLevel = request.ShortTermResultPressureLevel,
            EffectiveTrustThreshold = request.EffectiveTrustThreshold,
            KnowledgeStock = request.KnowledgeStock,
            KnowledgeDiversity = request.KnowledgeDiversity,
            ExternalShockLevel = request.ExternalShockLevel,
            CrossDomainExposure = request.CrossDomainExposure,
            RewiringSensitivity = request.RewiringSensitivity,
            EnableExternalShock = request.EnableExternalShock,
            ShockStep = request.ShockStep,
            ShockType = string.IsNullOrWhiteSpace(request.ShockType) ? ShockTypes.None : request.ShockType.Trim(),
            ShockDescription = request.ShockDescription?.Trim() ?? "",
            EnableChallengeEvent = request.EnableChallengeEvent,
            ChallengeStep = request.ChallengeStep,
            ChallengeLevel = request.ChallengeLevel,
            ChallengeType = string.IsNullOrWhiteSpace(request.ChallengeType) ? ChallengeTypes.None : request.ChallengeType.Trim(),
            ChallengeDescription = request.ChallengeDescription?.Trim() ?? "",
            RequiredKnowledgeDiversity = request.RequiredKnowledgeDiversity,
            RequiredCrossDomainExposure = request.RequiredCrossDomainExposure,
            RequiredRewiringScore = request.RequiredRewiringScore,
            ExplorationTendency = request.ExplorationTendency,
            SerendipitySensitivity = request.SerendipitySensitivity,
            KnowledgeRecombinationRate = request.KnowledgeRecombinationRate,
            SerendipityThreshold = request.SerendipityThreshold,
            EnableSerendipity = request.EnableSerendipity,
            EnableTrustDynamics = request.EnableTrustDynamics,
            TrustGrowthRate = request.TrustGrowthRate,
            TrustDecayRate = request.TrustDecayRate,
            TrustSaturationStrength = request.TrustSaturationStrength,
            TrustCapacity = request.TrustCapacity,
            TrustCapacityPenalty = request.TrustCapacityPenalty,
            DistrustPenalty = request.DistrustPenalty,
            ConstructiveCriticismBonus = request.ConstructiveCriticismBonus,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Experiment CreateExperimentFromScenario(
        Scenario scenario,
        string name,
        string description,
        int runCount,
        int agentCount,
        int totalSteps,
        string llmProvider,
        string llmModel)
    {
        return new Experiment
        {
            ScenarioId = scenario.Id,
            Name = name.Trim(),
            Description = description.Trim(),
            Purpose = scenario.Purpose.Trim(),
            BoundaryConditions = scenario.BoundaryConditions.Trim(),
            KpiDefinition = scenario.KpiDefinition.Trim(),
            AgentCount = agentCount,
            TotalSteps = totalSteps,
            RunCount = runCount,
            LlmProvider = string.IsNullOrWhiteSpace(llmProvider) ? LlmDefaults.Provider : llmProvider.Trim(),
            LlmModel = string.IsNullOrWhiteSpace(llmModel) ? LlmDefaults.MockModel : llmModel.Trim(),
            InformationSharingLevel = scenario.InformationSharingLevel,
            CooperationLevel = scenario.CooperationLevel,
            CompetitionLevel = scenario.CompetitionLevel,
            PsychologicalSafetyLevel = scenario.PsychologicalSafetyLevel,
            LearningOrientationLevel = scenario.LearningOrientationLevel,
            CustomerOrientationLevel = scenario.CustomerOrientationLevel,
            ShortTermResultPressureLevel = scenario.ShortTermResultPressureLevel,
            EffectiveTrustThreshold = scenario.EffectiveTrustThreshold,
            KnowledgeStock = scenario.KnowledgeStock,
            KnowledgeDiversity = scenario.KnowledgeDiversity,
            ExternalShockLevel = scenario.ExternalShockLevel,
            CrossDomainExposure = scenario.CrossDomainExposure,
            RewiringSensitivity = scenario.RewiringSensitivity,
            EnableExternalShock = scenario.EnableExternalShock,
            ShockStep = scenario.ShockStep,
            ShockType = string.IsNullOrWhiteSpace(scenario.ShockType) ? ShockTypes.None : scenario.ShockType.Trim(),
            ShockDescription = scenario.ShockDescription?.Trim() ?? "",
            EnableChallengeEvent = scenario.EnableChallengeEvent,
            ChallengeStep = scenario.ChallengeStep,
            ChallengeLevel = scenario.ChallengeLevel,
            ChallengeType = string.IsNullOrWhiteSpace(scenario.ChallengeType) ? ChallengeTypes.None : scenario.ChallengeType.Trim(),
            ChallengeDescription = scenario.ChallengeDescription?.Trim() ?? "",
            RequiredKnowledgeDiversity = scenario.RequiredKnowledgeDiversity,
            RequiredCrossDomainExposure = scenario.RequiredCrossDomainExposure,
            RequiredRewiringScore = scenario.RequiredRewiringScore,
            ExplorationTendency = scenario.ExplorationTendency,
            SerendipitySensitivity = scenario.SerendipitySensitivity,
            KnowledgeRecombinationRate = scenario.KnowledgeRecombinationRate,
            SerendipityThreshold = scenario.SerendipityThreshold,
            EnableSerendipity = scenario.EnableSerendipity,
            EnableTrustDynamics = scenario.EnableTrustDynamics,
            TrustGrowthRate = scenario.TrustGrowthRate,
            TrustDecayRate = scenario.TrustDecayRate,
            TrustSaturationStrength = scenario.TrustSaturationStrength,
            TrustCapacity = scenario.TrustCapacity,
            TrustCapacityPenalty = scenario.TrustCapacityPenalty,
            DistrustPenalty = scenario.DistrustPenalty,
            ConstructiveCriticismBonus = scenario.ConstructiveCriticismBonus,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Scenario CreateScenario(Scenario request)
    {
        return new Scenario
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Purpose = request.Purpose.Trim(),
            BoundaryConditions = request.BoundaryConditions.Trim(),
            KpiDefinition = request.KpiDefinition.Trim(),
            AgentCount = request.AgentCount,
            TotalSteps = request.TotalSteps,
            RunCount = request.RunCount,
            LlmProvider = string.IsNullOrWhiteSpace(request.LlmProvider) ? LlmDefaults.Provider : request.LlmProvider.Trim(),
            LlmModel = string.IsNullOrWhiteSpace(request.LlmModel) ? LlmDefaults.MockModel : request.LlmModel.Trim(),
            InformationSharingLevel = request.InformationSharingLevel,
            CooperationLevel = request.CooperationLevel,
            CompetitionLevel = request.CompetitionLevel,
            PsychologicalSafetyLevel = request.PsychologicalSafetyLevel,
            LearningOrientationLevel = request.LearningOrientationLevel,
            CustomerOrientationLevel = request.CustomerOrientationLevel,
            ShortTermResultPressureLevel = request.ShortTermResultPressureLevel,
            EffectiveTrustThreshold = request.EffectiveTrustThreshold,
            KnowledgeStock = request.KnowledgeStock,
            KnowledgeDiversity = request.KnowledgeDiversity,
            ExternalShockLevel = request.ExternalShockLevel,
            CrossDomainExposure = request.CrossDomainExposure,
            RewiringSensitivity = request.RewiringSensitivity,
            EnableExternalShock = request.EnableExternalShock,
            ShockStep = request.ShockStep,
            ShockType = string.IsNullOrWhiteSpace(request.ShockType) ? ShockTypes.None : request.ShockType.Trim(),
            ShockDescription = request.ShockDescription?.Trim() ?? "",
            EnableChallengeEvent = request.EnableChallengeEvent,
            ChallengeStep = request.ChallengeStep,
            ChallengeLevel = request.ChallengeLevel,
            ChallengeType = string.IsNullOrWhiteSpace(request.ChallengeType) ? ChallengeTypes.None : request.ChallengeType.Trim(),
            ChallengeDescription = request.ChallengeDescription?.Trim() ?? "",
            RequiredKnowledgeDiversity = request.RequiredKnowledgeDiversity,
            RequiredCrossDomainExposure = request.RequiredCrossDomainExposure,
            RequiredRewiringScore = request.RequiredRewiringScore,
            ExplorationTendency = request.ExplorationTendency,
            SerendipitySensitivity = request.SerendipitySensitivity,
            KnowledgeRecombinationRate = request.KnowledgeRecombinationRate,
            SerendipityThreshold = request.SerendipityThreshold,
            EnableSerendipity = request.EnableSerendipity,
            EnableTrustDynamics = request.EnableTrustDynamics,
            TrustGrowthRate = request.TrustGrowthRate,
            TrustDecayRate = request.TrustDecayRate,
            TrustSaturationStrength = request.TrustSaturationStrength,
            TrustCapacity = request.TrustCapacity,
            TrustCapacityPenalty = request.TrustCapacityPenalty,
            DistrustPenalty = request.DistrustPenalty,
            ConstructiveCriticismBonus = request.ConstructiveCriticismBonus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };
    }

    public static SimulationProject CreateProjectFromExperiment(Experiment experiment, int runNo)
    {
        var project = CreateProject(new CreateSimulationRequest
        {
            Name = $"{experiment.Name} Run {runNo}",
            Purpose = experiment.Purpose,
            BoundaryConditions = experiment.BoundaryConditions,
            KpiDefinition = experiment.KpiDefinition,
            AgentCount = experiment.AgentCount,
            TotalSteps = experiment.TotalSteps,
            LlmProvider = experiment.LlmProvider,
            LlmModel = experiment.LlmModel,
            InformationSharingLevel = experiment.InformationSharingLevel,
            CooperationLevel = experiment.CooperationLevel,
            CompetitionLevel = experiment.CompetitionLevel,
            PsychologicalSafetyLevel = experiment.PsychologicalSafetyLevel,
            LearningOrientationLevel = experiment.LearningOrientationLevel,
            CustomerOrientationLevel = experiment.CustomerOrientationLevel,
            ShortTermResultPressureLevel = experiment.ShortTermResultPressureLevel,
            EffectiveTrustThreshold = experiment.EffectiveTrustThreshold,
            KnowledgeStock = experiment.KnowledgeStock,
            KnowledgeDiversity = experiment.KnowledgeDiversity,
            ExternalShockLevel = experiment.ExternalShockLevel,
            CrossDomainExposure = experiment.CrossDomainExposure,
            RewiringSensitivity = experiment.RewiringSensitivity,
            EnableExternalShock = experiment.EnableExternalShock,
            ShockStep = experiment.ShockStep,
            ShockType = experiment.ShockType,
            ShockDescription = experiment.ShockDescription,
            EnableChallengeEvent = experiment.EnableChallengeEvent,
            ChallengeStep = experiment.ChallengeStep,
            ChallengeLevel = experiment.ChallengeLevel,
            ChallengeType = experiment.ChallengeType,
            ChallengeDescription = experiment.ChallengeDescription,
            RequiredKnowledgeDiversity = experiment.RequiredKnowledgeDiversity,
            RequiredCrossDomainExposure = experiment.RequiredCrossDomainExposure,
            RequiredRewiringScore = experiment.RequiredRewiringScore,
            ExplorationTendency = experiment.ExplorationTendency,
            SerendipitySensitivity = experiment.SerendipitySensitivity,
            KnowledgeRecombinationRate = experiment.KnowledgeRecombinationRate,
            SerendipityThreshold = experiment.SerendipityThreshold,
            EnableSerendipity = experiment.EnableSerendipity,
            EnableTrustDynamics = experiment.EnableTrustDynamics,
            TrustGrowthRate = experiment.TrustGrowthRate,
            TrustDecayRate = experiment.TrustDecayRate,
            TrustSaturationStrength = experiment.TrustSaturationStrength,
            TrustCapacity = experiment.TrustCapacity,
            TrustCapacityPenalty = experiment.TrustCapacityPenalty,
            DistrustPenalty = experiment.DistrustPenalty,
            ConstructiveCriticismBonus = experiment.ConstructiveCriticismBonus
        });

        project.ExperimentId = experiment.Id;
        return project;
    }
}
