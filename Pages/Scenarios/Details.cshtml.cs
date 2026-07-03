using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Scenarios;

public sealed class DetailsModel(AppDbContext db) : PageModel
{
    public Scenario? Scenario { get; private set; }
    public int ExperimentCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Scenario = await db.Scenarios.FirstOrDefaultAsync(item => item.Id == id);
        if (Scenario is null)
        {
            return RedirectToPage("/Scenarios/Index");
        }

        ExperimentCount = await db.Experiments.CountAsync(item => item.ScenarioId == id);
        return Page();
    }
}
