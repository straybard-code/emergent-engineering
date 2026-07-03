using EmergentEngineering.Data;
using EmergentEngineering.Models;
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
        scenario.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return RedirectToPage("/Scenarios/Details", new { id = scenario.Id });
    }
}
