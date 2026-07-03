using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Experiments;

public sealed class IndexModel(AppDbContext db) : PageModel
{
    public List<Experiment> Experiments { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Experiments = await db.Experiments
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
    }
}
