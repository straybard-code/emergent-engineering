using EmergentEngineering.Data;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class DeleteModel(AppDbContext db) : PageModel
{
    public EmergentEngineering.Models.PhaseDiagram? Diagram { get; private set; }
    public int PointCount { get; private set; }
    public int ExperimentCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Diagram = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == id);
        if (Diagram is null)
        {
            return RedirectToPage("/PhaseDiagrams/Index");
        }

        await LoadCountsAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var diagram = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == id);
        if (diagram is null)
        {
            return RedirectToPage("/PhaseDiagrams/Index");
        }

        var points = await db.PhaseDiagramPoints
            .Where(item => item.PhaseDiagramId == id)
            .ToListAsync();

        var experimentIds = points
            .Where(item => item.ExperimentId.HasValue)
            .Select(item => item.ExperimentId!.Value)
            .Distinct()
            .ToList();

        foreach (var experimentId in experimentIds)
        {
            await SimulationDataDeletion.DeleteExperimentAsync(db, experimentId);
        }

        if (points.Count > 0)
        {
            db.PhaseDiagramPoints.RemoveRange(points);
            await db.SaveChangesAsync();
        }

        db.PhaseDiagrams.Remove(diagram);
        await db.SaveChangesAsync();
        return RedirectToPage("/PhaseDiagrams/Index");
    }

    private async Task LoadCountsAsync(int phaseDiagramId)
    {
        var points = await db.PhaseDiagramPoints
            .Where(item => item.PhaseDiagramId == phaseDiagramId)
            .ToListAsync();

        PointCount = points.Count;
        ExperimentCount = points.Where(item => item.ExperimentId.HasValue).Select(item => item.ExperimentId!.Value).Distinct().Count();
    }
}
