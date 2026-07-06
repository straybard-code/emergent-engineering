using EmergentEngineering.Data;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class RunModel(AppDbContext db, PhaseDiagramRunner phaseDiagramRunner) : PageModel
{
    [TempData]
    public string? DiagramMessage { get; set; }

    public EmergentEngineering.Models.PhaseDiagram? Diagram { get; private set; }
    public string ScenarioName { get; private set; } = "-";
    public int GridPointCount { get; private set; }
    public int EstimatedExecutionCount { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Diagram = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == id);
        if (Diagram is null)
        {
            return RedirectToPage("/PhaseDiagrams/Index");
        }

        ScenarioName = await db.Scenarios
            .Where(item => item.Id == Diagram.BaseScenarioId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync() ?? "-";

        GridPointCount = CalculatePointCount(Diagram.XStartValue, Diagram.XEndValue, Diagram.XStepValue)
            * CalculatePointCount(Diagram.YStartValue, Diagram.YEndValue, Diagram.YStepValue);
        EstimatedExecutionCount = GridPointCount * Math.Max(1, Diagram.RunsPerPoint);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        try
        {
            await phaseDiagramRunner.RunAsync(id);
        }
        catch
        {
            DiagramMessage = "相図実行中にエラーが発生したため、状態を Failed に更新しました。";
        }

        return RedirectToPage("/PhaseDiagrams/Details", new { id });
    }

    private static int CalculatePointCount(double startValue, double endValue, double stepValue)
    {
        if (stepValue <= 0)
        {
            return 0;
        }

        var count = 0;
        var start = decimal.Round((decimal)startValue, 3);
        var end = decimal.Round((decimal)endValue, 3);
        var step = decimal.Round((decimal)stepValue, 3);

        for (var value = start; value <= end + 0.0001m; value += step)
        {
            count++;
        }

        return count;
    }
}
