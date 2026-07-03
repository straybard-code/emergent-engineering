using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.Experiments;

public sealed class RunModel(AppDbContext db, IExperimentExecutionService experimentExecutionService) : PageModel
{
    public Experiment? Experiment { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Experiment = await db.Experiments.FirstOrDefaultAsync(item => item.Id == id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var experiment = await experimentExecutionService.RunExperimentAsync(id);
        if (experiment is null)
        {
            return Page();
        }

        if (experiment.Status == ExperimentStatus.Stopped)
        {
            TempData["ExperimentMessage"] = "停止要求を受け付けたため、現在の Run 完了後に実験を停止しました。";
        }

        return RedirectToPage("/Experiments/Details", new { id = experiment.Id });
    }
}
