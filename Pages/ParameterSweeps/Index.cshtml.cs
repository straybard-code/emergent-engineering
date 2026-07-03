using EmergentEngineering.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class IndexModel(AppDbContext db) : PageModel
{
    public List<ParameterSweepListRow> Sweeps { get; private set; } = [];

    public async Task OnGetAsync()
    {
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
}
