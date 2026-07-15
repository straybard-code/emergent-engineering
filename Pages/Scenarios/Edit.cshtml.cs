using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Scenarios;

public sealed class EditModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Scenario Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var scenario = await db.Scenarios.FirstOrDefaultAsync(item => item.Id == id);
        if (scenario is null)
        {
            return RedirectToPage("/Scenarios/Index");
        }

        Input = scenario;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var scenario = await db.Scenarios.FirstOrDefaultAsync(item => item.Id == id);
        if (scenario is null)
        {
            return RedirectToPage("/Scenarios/Index");
        }

        scenario.Name = Input.Name.Trim();
        scenario.Description = Input.Description.Trim();
        scenario.Purpose = Input.Purpose.Trim();
        scenario.BoundaryConditions = Input.BoundaryConditions.Trim();
        scenario.KpiDefinition = Input.KpiDefinition.Trim();
        scenario.AgentCount = Input.AgentCount;
        scenario.TotalSteps = Input.TotalSteps;
        scenario.RunCount = Input.RunCount;
        scenario.LlmProvider = Input.LlmProvider.Trim();
        scenario.LlmModel = Input.LlmModel.Trim();
        scenario.InformationSharingLevel = Input.InformationSharingLevel;
        scenario.CooperationLevel = Input.CooperationLevel;
        scenario.CompetitionLevel = Input.CompetitionLevel;
        scenario.PsychologicalSafetyLevel = Input.PsychologicalSafetyLevel;
        scenario.LearningOrientationLevel = Input.LearningOrientationLevel;
        scenario.CustomerOrientationLevel = Input.CustomerOrientationLevel;
        scenario.ShortTermResultPressureLevel = Input.ShortTermResultPressureLevel;
        scenario.EffectiveTrustThreshold = Input.EffectiveTrustThreshold;
        scenario.KnowledgeStock = Input.KnowledgeStock;
        scenario.KnowledgeDiversity = Input.KnowledgeDiversity;
        scenario.ExternalShockLevel = Input.ExternalShockLevel;
        scenario.CrossDomainExposure = Input.CrossDomainExposure;
        scenario.RewiringSensitivity = Input.RewiringSensitivity;
        scenario.EnableExternalShock = Input.EnableExternalShock;
        scenario.ShockStep = Input.ShockStep;
        scenario.ShockType = string.IsNullOrWhiteSpace(Input.ShockType) ? ShockTypes.None : Input.ShockType.Trim();
        scenario.ShockDescription = Input.ShockDescription?.Trim() ?? "";
        scenario.EnableChallengeEvent = Input.EnableChallengeEvent;
        scenario.ChallengeStep = Input.ChallengeStep;
        scenario.ChallengeLevel = Input.ChallengeLevel;
        scenario.ChallengeType = string.IsNullOrWhiteSpace(Input.ChallengeType) ? ChallengeTypes.None : Input.ChallengeType.Trim();
        scenario.ChallengeDescription = Input.ChallengeDescription?.Trim() ?? "";
        scenario.RequiredKnowledgeDiversity = Input.RequiredKnowledgeDiversity;
        scenario.RequiredCrossDomainExposure = Input.RequiredCrossDomainExposure;
        scenario.RequiredRewiringScore = Input.RequiredRewiringScore;
        scenario.ExplorationTendency = Input.ExplorationTendency;
        scenario.SerendipitySensitivity = Input.SerendipitySensitivity;
        scenario.KnowledgeRecombinationRate = Input.KnowledgeRecombinationRate;
        scenario.SerendipityThreshold = Input.SerendipityThreshold;
        scenario.EnableSerendipity = Input.EnableSerendipity;
        scenario.EnableTrustDynamics = Input.EnableTrustDynamics;
        scenario.TrustGrowthRate = Input.TrustGrowthRate;
        scenario.TrustDecayRate = Input.TrustDecayRate;
        scenario.TrustSaturationStrength = Input.TrustSaturationStrength;
        scenario.TrustCapacity = Input.TrustCapacity;
        scenario.TrustCapacityPenalty = Input.TrustCapacityPenalty;
        scenario.DistrustPenalty = Input.DistrustPenalty;
        scenario.ConstructiveCriticismBonus = Input.ConstructiveCriticismBonus;
        scenario.EnableThanksCoin = Input.EnableThanksCoin;
        scenario.ThanksCoinRate = Input.ThanksCoinRate;
        scenario.ThanksCoinRespectGain = Input.ThanksCoinRespectGain;
        scenario.ThanksCoinTrustGain = Input.ThanksCoinTrustGain;
        scenario.ThanksCoinReconfigurationGain = Input.ThanksCoinReconfigurationGain;
        scenario.ThanksCoinPsychologicalSafetyGain = Input.ThanksCoinPsychologicalSafetyGain;
        scenario.ThanksCoinBridgeGain = Input.ThanksCoinBridgeGain;
        scenario.ThanksCoinPopularityBias = Input.ThanksCoinPopularityBias;
        scenario.ThanksCoinDiversityBonus = Input.ThanksCoinDiversityBonus;
        scenario.ThanksCoinChallengeBonus = Input.ThanksCoinChallengeBonus;
        scenario.EnableThinkingSpeedModel = Input.EnableThinkingSpeedModel;
        scenario.ThinkingSpeedBase = Input.ThinkingSpeedBase;
        scenario.ThinkingSpeedDispersion = Input.ThinkingSpeedDispersion;
        scenario.OrganizationalDecisionSpeed = Input.OrganizationalDecisionSpeed;
        scenario.OrganizationalValidationSpeed = Input.OrganizationalValidationSpeed;
        scenario.EnableIntellectualRespect = Input.EnableIntellectualRespect;
        scenario.IntellectualRespectBase = Input.IntellectualRespectBase;
        scenario.IntellectualRespectGrowthRate = Input.IntellectualRespectGrowthRate;
        scenario.IntellectualRespectDecayRate = Input.IntellectualRespectDecayRate;
        scenario.IntellectualRespectDiversitySensitivity = Input.IntellectualRespectDiversitySensitivity;
        scenario.IntellectualRespectChallengeSensitivity = Input.IntellectualRespectChallengeSensitivity;
        scenario.IntellectualRespectMentorshipSensitivity = Input.IntellectualRespectMentorshipSensitivity;
        scenario.IntellectualRespectEgoPenalty = Input.IntellectualRespectEgoPenalty;
        scenario.IntellectualRespectHierarchyPenalty = Input.IntellectualRespectHierarchyPenalty;
        SimulationFactory.NormalizeOptionalEventSettings(scenario);
        scenario.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return RedirectToPage("/Scenarios/Details", new { id = scenario.Id });
    }
}
