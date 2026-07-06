using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Experiments;

public sealed class IndexModel(AppDbContext db, ExperimentCleanupService cleanupService, ParameterSweepRunner parameterSweepRunner) : PageModel
{
    [BindProperty]
    public List<int> SelectedExperimentIds { get; set; } = [];

    [TempData]
    public string? CleanupMessage { get; set; }

    public List<Experiment> Experiments { get; private set; } = [];
    public int PendingCount { get; private set; }
    public int RunningCount { get; private set; }
    public int CompletedCount { get; private set; }
    public int FailedCount { get; private set; }
    public int CancelledCount { get; private set; }

    public async Task OnGetAsync()
    {
        Experiments = await db.Experiments
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        PendingCount = Experiments.Count(item => GetStatusCategory(item.Status) == ExperimentStatusCategory.Pending);
        RunningCount = Experiments.Count(item => GetStatusCategory(item.Status) == ExperimentStatusCategory.Running);
        CompletedCount = Experiments.Count(item => GetStatusCategory(item.Status) == ExperimentStatusCategory.Completed);
        FailedCount = Experiments.Count(item => GetStatusCategory(item.Status) == ExperimentStatusCategory.Failed);
        CancelledCount = Experiments.Count(item => GetStatusCategory(item.Status) == ExperimentStatusCategory.Cancelled);
    }

    public async Task<IActionResult> OnPostDeleteSelectedAsync(CancellationToken cancellationToken)
    {
        if (SelectedExperimentIds.Count == 0)
        {
            CleanupMessage = "削除するExperimentを選択してください。";
            return RedirectToPage();
        }

        var result = await cleanupService.DeleteExperimentsAsync(
            SelectedExperimentIds,
            async (sweepId, innerCancellationToken) =>
            {
                await parameterSweepRunner.RecalculateSweepStatusAsync(sweepId, innerCancellationToken);
            },
            cancellationToken);

        CleanupMessage = result.ToJapaneseMessage();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkFailedAsync(int id)
    {
        var updated = await cleanupService.MarkExperimentAsFailedAsync(id);
        CleanupMessage = updated
            ? "Experimentを失敗扱いにしました。"
            : "Experimentが見つかりませんでした。";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForceDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var result = await cleanupService.ForceDeleteExperimentAsync(
            id,
            async (sweepId, innerCancellationToken) =>
            {
                await parameterSweepRunner.RecalculateSweepStatusAsync(sweepId, innerCancellationToken);
            },
            cancellationToken);

        CleanupMessage = result.DeletedExperiments > 0
            ? result.ToJapaneseMessage()
            : "削除対象のExperimentがありませんでした。";
        return RedirectToPage();
    }

    public static ExperimentStatusCategory GetStatusCategory(string? status) => status switch
    {
        "Created" => ExperimentStatusCategory.Pending,
        "Running" => ExperimentStatusCategory.Running,
        "Completed" => ExperimentStatusCategory.Completed,
        "Failed" => ExperimentStatusCategory.Failed,
        "StopRequested" => ExperimentStatusCategory.Cancelled,
        "Stopped" => ExperimentStatusCategory.Cancelled,
        "Cancelled" => ExperimentStatusCategory.Cancelled,
        _ => ExperimentStatusCategory.Pending
    };

    public static bool IsDeletionBlocked(string? status)
    {
        return string.Equals(status, ExperimentStatus.Running, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ExperimentStatus.StopRequested, StringComparison.OrdinalIgnoreCase);
    }
}

public enum ExperimentStatusCategory
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
