using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Experiments;

public sealed class IndexModel(AppDbContext db) : PageModel
{
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
}

public enum ExperimentStatusCategory
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
