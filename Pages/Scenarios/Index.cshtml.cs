using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Scenarios;

public sealed class IndexModel(AppDbContext db) : PageModel
{
    public List<Scenario> Scenarios { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Scenarios = await db.Scenarios
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
    }
}
