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
        scenario.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return RedirectToPage("/Scenarios/Details", new { id = scenario.Id });
    }
}
