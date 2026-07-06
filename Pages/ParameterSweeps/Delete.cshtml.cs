using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class DeleteModel(AppDbContext db) : PageModel
{
    public ParameterSweep? Sweep { get; private set; }
    public int SweepRunCount { get; private set; }
    public int ExperimentCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
        if (Sweep is null)
        {
            return RedirectToPage("/ParameterSweeps/Index");
        }

        await LoadCountsAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
        if (sweep is null)
        {
            return RedirectToPage("/ParameterSweeps/Index");
        }

        if (string.Equals(sweep.Status, ParameterSweepStatus.Running, StringComparison.OrdinalIgnoreCase))
        {
            TempData["SweepMessage"] = "実行中のParameter Sweepは通常削除できません。失敗扱いにするか強制削除してください。";
            return RedirectToPage("/ParameterSweeps/Details", new { id });
        }

        var runs = await db.ParameterSweepRuns
            .Where(item => item.ParameterSweepId == id)
            .ToListAsync();

        var experimentIds = runs
            .Select(item => item.ExperimentId)
            .Distinct()
            .ToList();

        foreach (var experimentId in experimentIds)
        {
            await SimulationDataDeletion.DeleteExperimentAsync(db, experimentId);
        }

        if (runs.Count > 0)
        {
            db.ParameterSweepRuns.RemoveRange(runs);
            await db.SaveChangesAsync();
        }

        db.ParameterSweeps.Remove(sweep);
        await db.SaveChangesAsync();
        return RedirectToPage("/ParameterSweeps/Index");
    }

    private async Task LoadCountsAsync(int parameterSweepId)
    {
        var runs = await db.ParameterSweepRuns
            .Where(item => item.ParameterSweepId == parameterSweepId)
            .ToListAsync();

        SweepRunCount = runs.Count;
        ExperimentCount = runs.Select(item => item.ExperimentId).Distinct().Count();
    }
}
