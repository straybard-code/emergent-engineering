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
        Purpose = "小さなAI組織が、境界条件とKPIに応じてどのように組織化されるかを観察する。",
        BoundaryConditions = "情報共有を重視する。困ったときは相談してよい。失敗は学習材料として扱う。互いに支援することを推奨する。",
        KpiDefinition = "市場学習、品質、リピート率、顧客成功",
        AgentCount = 4,
        TotalSteps = 5
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
