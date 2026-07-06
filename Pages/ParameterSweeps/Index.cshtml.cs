using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class IndexModel(
    AppDbContext db,
    ParameterSweepRunner sweepRunner,
    ExperimentCleanupService cleanupService) : PageModel
{
    [TempData]
    public string? SweepMessage { get; set; }

    public List<ParameterSweepListRow> Sweeps { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var runningSweepIds = await db.ParameterSweeps
            .Where(item => item.Status == ParameterSweepStatus.Running)
            .Select(item => item.Id)
            .ToListAsync();

        foreach (var sweepId in runningSweepIds)
        {
            try
            {
                await sweepRunner.RecalculateSweepStatusAsync(sweepId);
            }
            catch
            {
                // If recalculation fails, fall back to the stored status.
            }
        }

        var sweeps = await db.ParameterSweeps
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        var scenarioNames = await db.Scenarios
            .ToDictionaryAsync(item => item.Id, item => item.Name);

        Sweeps = sweeps
            .Select(item => new ParameterSweepListRow
            {
                Id = item.Id,
                Name = item.Name,
                ScenarioName = scenarioNames.GetValueOrDefault(item.ScenarioId, "-"),
                TargetParameter = item.TargetParameter,
                Status = item.Status,
                CreatedAt = item.CreatedAt
            })
            .ToList();
    }

    public async Task<IActionResult> OnPostMarkFailedAsync(int id)
    {
        var updated = await cleanupService.MarkParameterSweepAsFailedAsync(id);
        SweepMessage = updated
            ? "Parameter Sweepを失敗扱いにしました。"
            : "Parameter Sweepが見つかりませんでした。";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForceDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var result = await cleanupService.ForceDeleteParameterSweepAsync(id, cancellationToken);
        SweepMessage = result.DeletedParameterSweeps > 0
            ? result.ToJapaneseMessage()
            : "削除対象のParameter Sweepがありませんでした。";
        return RedirectToPage();
    }
}
