using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class IndexModel(AppDbContext db, PhaseDiagramRunner phaseDiagramRunner) : PageModel
{
    public List<PhaseDiagramListRow> Diagrams { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var runningIds = await db.PhaseDiagrams
            .Where(item => item.Status == PhaseDiagramStatus.Running)
            .Select(item => item.Id)
            .ToListAsync();

        foreach (var id in runningIds)
        {
            try
            {
                await phaseDiagramRunner.RecalculatePhaseDiagramStatusAsync(id);
            }
            catch
            {
                // Fall back to stored status if recalculation fails.
            }
        }

        var diagrams = await db.PhaseDiagrams
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        var scenarioNames = await db.Scenarios.ToDictionaryAsync(item => item.Id, item => item.Name);
        var phaseDiagramNames = await db.PhaseDiagrams.ToDictionaryAsync(item => item.Id, item => item.Name);

        Diagrams = diagrams
            .Select(item =>
            {
                var xCount = BuildParameterValues(item.XStartValue, item.XEndValue, item.XStepValue).Count;
                var yCount = BuildParameterValues(item.YStartValue, item.YEndValue, item.YStepValue).Count;

                return new PhaseDiagramListRow
                {
                    Id = item.Id,
                    Name = item.Name,
                    ScenarioName = scenarioNames.GetValueOrDefault(item.BaseScenarioId, "-"),
                    XAxisLabel = item.XParameterDisplayName,
                    YAxisLabel = item.YParameterDisplayName,
                    GridPointCount = xCount * yCount,
                    IsAdaptiveSweep = item.IsAdaptiveSweep,
                    ParentPhaseDiagramId = item.ParentPhaseDiagramId,
                    ParentPhaseDiagramName = item.ParentPhaseDiagramId.HasValue
                        ? phaseDiagramNames.GetValueOrDefault(item.ParentPhaseDiagramId.Value, "-")
                        : "-",
                    Status = item.Status,
                    CreatedAt = item.CreatedAt
                };
            })
            .ToList();
    }

    private static List<double> BuildParameterValues(double startValue, double endValue, double stepValue)
    {
        List<double> values = [];
        var start = decimal.Round((decimal)startValue, 3);
        var end = decimal.Round((decimal)endValue, 3);
        var step = decimal.Round((decimal)stepValue, 3);

        for (var value = start; value <= end + 0.0001m; value += step)
        {
            values.Add(Math.Round((double)value, 3));
        }

        return values;
    }
}
