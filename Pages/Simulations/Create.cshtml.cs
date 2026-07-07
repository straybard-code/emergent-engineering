using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmergentEngineering.Pages.Simulations;

public sealed class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public CreateSimulationRequest Input { get; set; } = new()
    {
        Name = "Emergence Engineering Lab MVP",
        Purpose = "Observe how a small AI team self-organizes under a shared goal, boundary conditions, and KPI.",
        BoundaryConditions = "Information sharing is encouraged. Asking for help is acceptable. Feedback should remain constructive and support learning.",
        KpiDefinition = "Market learning, quality, repeat use, customer success",
        AgentCount = 4,
        TotalSteps = 5,
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
        EnableSerendipity = SerendipityDefaults.EnableSerendipity,
        EnableTrustDynamics = TrustDynamicsDefaults.EnableTrustDynamics,
        TrustGrowthRate = TrustDynamicsDefaults.TrustGrowthRate,
        TrustDecayRate = TrustDynamicsDefaults.TrustDecayRate,
        TrustSaturationStrength = TrustDynamicsDefaults.TrustSaturationStrength,
        TrustCapacity = TrustDynamicsDefaults.TrustCapacity,
        TrustCapacityPenalty = TrustDynamicsDefaults.TrustCapacityPenalty,
        DistrustPenalty = TrustDynamicsDefaults.DistrustPenalty,
        ConstructiveCriticismBonus = TrustDynamicsDefaults.ConstructiveCriticismBonus,
        LogDetailLevel = LogDetailLevels.Full,
        StepLogInterval = LogRetentionDefaults.FullStepInterval,
        ActionLogInterval = LogRetentionDefaults.FullActionInterval
    };

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var project = SimulationFactory.CreateProject(Input);
        db.SimulationProjects.Add(project);
        await db.SaveChangesAsync();

        return RedirectToPage("/Simulations/Details", new { id = project.Id });
    }
}
