using System.Data.Common;
using EmergentEngineering.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Admin;

public sealed class DataCleanupModel(AppDbContext db) : PageModel
{
    private const string BulkDeletionDisabledMessage =
        "\u4e00\u62ec\u524a\u9664\u306f\u89aa\u5b50\u95a2\u4fc2\u304c\u8907\u96d1\u306a\u305f\u3081\u73fe\u5728\u505c\u6b62\u4e2d\u3067\u3059\u3002Experiment\u8a73\u7d30\u753b\u9762\u304b\u3089\u500b\u5225\u306b\u524a\u9664\u3057\u3066\u304f\u3060\u3055\u3044\u3002";

    [BindProperty]
    public DateTime CutoffDate { get; set; } = DateTime.UtcNow.AddDays(-30);

    public int SimulationCount { get; private set; }
    public int ExperimentCount { get; private set; }
    public int AgentCount { get; private set; }
    public int SimulationStepCount { get; private set; }
    public int AgentActionCount { get; private set; }
    public int TrustSnapshotCount { get; private set; }
    public double DatabaseUsageMb { get; private set; }

    [TempData]
    public string? CleanupMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadCountsAsync();
    }

    public Task<IActionResult> OnPostDeleteCompletedSimulationsAsync(CancellationToken cancellationToken)
        => ReturnBulkDeletionDisabledAsync();

    public Task<IActionResult> OnPostDeleteFailedSimulationsAsync(CancellationToken cancellationToken)
        => ReturnBulkDeletionDisabledAsync();

    public Task<IActionResult> OnPostFailRunningSimulationsAsync(CancellationToken cancellationToken)
        => ReturnBulkDeletionDisabledAsync();

    public Task<IActionResult> OnPostDeleteOldRunsAsync(CancellationToken cancellationToken)
        => ReturnBulkDeletionDisabledAsync();

    public Task<IActionResult> OnPostPruneDetailedLogsAsync(CancellationToken cancellationToken)
        => ReturnBulkDeletionDisabledAsync();

    private Task<IActionResult> ReturnBulkDeletionDisabledAsync()
    {
        CleanupMessage = BulkDeletionDisabledMessage;
        IActionResult result = RedirectToPage();
        return Task.FromResult(result);
    }

    private async Task LoadCountsAsync()
    {
        SimulationCount = await db.SimulationProjects.CountAsync();
        ExperimentCount = await db.Experiments.CountAsync();
        AgentCount = await db.Agents.CountAsync();
        SimulationStepCount = await db.SimulationSteps.CountAsync();
        AgentActionCount = await db.AgentActions.CountAsync();
        TrustSnapshotCount = await db.TrustSnapshots.CountAsync();
        DatabaseUsageMb = await LoadDatabaseUsageMbAsync();
    }

    private async Task<double> LoadDatabaseUsageMbAsync()
    {
        DbConnection connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await db.Database.OpenConnectionAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CAST(SUM(size) * 8.0 / 1024 AS float)
            FROM sys.master_files
            WHERE database_id = DB_ID();
            """;

        try
        {
            var value = await command.ExecuteScalarAsync();
            return value is null || value is DBNull ? 0 : Convert.ToDouble(value);
        }
        finally
        {
            if (shouldClose)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}