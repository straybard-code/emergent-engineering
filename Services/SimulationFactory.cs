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
            ShortTermResultPressureLevel = experiment.ShortTermResultPressureLevel
        });

        project.ExperimentId = experiment.Id;
        return project;
    }
}
