using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages;

public class IndexModel(
    AppDbContext db,
    ExperimentCleanupService cleanupService,
    ParameterSweepRunner sweepRunner) : PageModel
{
    [TempData]
    public string? SimulationMessage { get; set; }

    public List<SimulationProject> Projects { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Projects = await db.SimulationProjects
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostMarkFailedAsync(int id)
    {
        var updated = await cleanupService.MarkSimulationAsFailedAsync(id);
        SimulationMessage = updated
            ? "Simulationを失敗扱いにしました。"
            : "Simulationが見つかりませんでした。";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForceDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var result = await cleanupService.ForceDeleteSimulationAsync(
            id,
            async (sweepId, innerCancellationToken) =>
            {
                await sweepRunner.RecalculateSweepStatusAsync(sweepId, innerCancellationToken);
            },
            cancellationToken);

        SimulationMessage = result.DeletedSimulationProjects > 0
            ? result.ToJapaneseMessage()
            : "削除対象のSimulationがありませんでした。";
        return RedirectToPage();
    }
}
