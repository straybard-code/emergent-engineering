using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Experiments;

public sealed class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? ScenarioId { get; set; }

    [BindProperty]
    public CreateExperimentRequest Input { get; set; } = new()
    {
        Name = "Emergence Engineering Comparison",
        Description = "Repeated runs of the same organization setup to compare phase and trust behavior.",
        Purpose = "Compare how the same condition set evolves across multiple independent runs.",
        BoundaryConditions = "Information sharing is encouraged. Help-seeking is acceptable. Feedback should remain constructive.",
        KpiDefinition = "Market learning, quality, repeat use, customer success",
        AgentCount = 4,
        TotalSteps = 5,
        RunCount = 3,
        LlmProvider = LlmProviderType.Mock,
        LlmModel = LlmDefaults.MockModel,
        InformationSharingLevel = BoundaryParameterDefaults.Level,
        CooperationLevel = BoundaryParameterDefaults.Level,
        CompetitionLevel = BoundaryParameterDefaults.Level,
        PsychologicalSafetyLevel = BoundaryParameterDefaults.Level,
        LearningOrientationLevel = BoundaryParameterDefaults.Level,
        CustomerOrientationLevel = BoundaryParameterDefaults.Level,
        ShortTermResultPressureLevel = BoundaryParameterDefaults.Level,
        EffectiveTrustThreshold = BoundaryParameterDefaults.EffectiveTrustThreshold,
        KnowledgeStock = KnowledgeDefaults.Stock,
        KnowledgeDiversity = KnowledgeDefaults.Diversity,
        ExternalShockLevel = KnowledgeDefaults.ExternalShockLevel,
        CrossDomainExposure = KnowledgeDefaults.CrossDomainExposure,
        RewiringSensitivity = KnowledgeDefaults.RewiringSensitivity,
        EnableExternalShock = KnowledgeDefaults.EnableExternalShock,
        ShockStep = KnowledgeDefaults.ShockStep,
        ShockType = KnowledgeDefaults.ShockType,
        ShockDescription = KnowledgeDefaults.ShockDescription,
        EnableChallengeEvent = ChallengeDefaults.EnableChallengeEvent,
        ChallengeType = ChallengeDefaults.ChallengeType,
        ChallengeStep = ChallengeDefaults.ChallengeStep,
        ChallengeLevel = ChallengeDefaults.ChallengeLevel,
        ChallengeDescription = ChallengeDefaults.ChallengeDescription,
        RequiredKnowledgeDiversity = ChallengeDefaults.RequiredKnowledgeDiversity,
        RequiredCrossDomainExposure = ChallengeDefaults.RequiredCrossDomainExposure,
        RequiredRewiringScore = ChallengeDefaults.RequiredRewiringScore,
        ExplorationTendency = SerendipityDefaults.ExplorationTendency,
        SerendipitySensitivity = SerendipityDefaults.SerendipitySensitivity,
        KnowledgeRecombinationRate = SerendipityDefaults.KnowledgeRecombinationRate,
        SerendipityThreshold = SerendipityDefaults.SerendipityThreshold,
        EnableSerendipity = SerendipityDefaults.EnableSerendipity
    };

    public List<ScenarioOption> Scenarios { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadScenariosAsync();

        if (!ScenarioId.HasValue)
        {
            return;
        }

        var scenario = await db.Scenarios.FirstOrDefaultAsync(item => item.Id == ScenarioId.Value);
        if (scenario is null)
        {
            return;
        }

        Input = MapScenario(scenario);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadScenariosAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var experiment = SimulationFactory.CreateExperiment(Input);
        experiment.ScenarioId = ScenarioId;
        db.Experiments.Add(experiment);
        await db.SaveChangesAsync();

        return RedirectToPage("/Experiments/Details", new { id = experiment.Id });
    }

    private async Task LoadScenariosAsync()
    {
        Scenarios = await db.Scenarios
            .OrderBy(item => item.Name)
            .Select(item => new ScenarioOption(item.Id, item.Name))
            .ToListAsync();
    }

    private static CreateExperimentRequest MapScenario(Scenario scenario)
    {
        return new CreateExperimentRequest
        {
            Name = scenario.Name,
            Description = scenario.Description,
            Purpose = scenario.Purpose,
            BoundaryConditions = scenario.BoundaryConditions,
            KpiDefinition = scenario.KpiDefinition,
            AgentCount = scenario.AgentCount,
            TotalSteps = scenario.TotalSteps,
            RunCount = scenario.RunCount,
            LlmProvider = scenario.LlmProvider,
            LlmModel = scenario.LlmModel,
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
            ShockType = scenario.ShockType,
            ShockDescription = scenario.ShockDescription,
            EnableChallengeEvent = scenario.EnableChallengeEvent,
            ChallengeType = scenario.ChallengeType,
            ChallengeStep = scenario.ChallengeStep,
            ChallengeLevel = scenario.ChallengeLevel,
            ChallengeDescription = scenario.ChallengeDescription,
            RequiredKnowledgeDiversity = scenario.RequiredKnowledgeDiversity,
            RequiredCrossDomainExposure = scenario.RequiredCrossDomainExposure,
            RequiredRewiringScore = scenario.RequiredRewiringScore,
            ExplorationTendency = scenario.ExplorationTendency,
            SerendipitySensitivity = scenario.SerendipitySensitivity,
            KnowledgeRecombinationRate = scenario.KnowledgeRecombinationRate,
            SerendipityThreshold = scenario.SerendipityThreshold,
            EnableSerendipity = scenario.EnableSerendipity
        };
    }

    public sealed record ScenarioOption(int Id, string Name);
}
