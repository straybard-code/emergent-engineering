using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<SimulationProject> Projects { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Projects = await db.SimulationProjects
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync();
    }
}
