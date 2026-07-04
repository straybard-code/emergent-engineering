using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public ParameterSweep Input { get; set; } = new()
    {
        Name = "信頼成長率スイープ",
        Description = "基準シナリオに対して1パラメータのみを変化させる感度分析",
        TargetParameter = BoundaryParameterNames.TrustGrowthRate,
        StartValue = 0.2,
        EndValue = 0.8,
        StepValue = 0.1,
        RunCountPerValue = 5,
        AgentCount = 10,
        TotalSteps = 50,
        LlmProvider = LlmProviderType.Mock,
        LlmModel = LlmDefaults.MockModel
    };

    public List<ScenarioOption> Scenarios { get; private set; } = [];

    public async Task OnGetAsync()
    {
        await LoadScenariosAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadScenariosAsync();
        ValidateInput();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var sweep = new ParameterSweep
        {
            Name = Input.Name.Trim(),
            Description = Input.Description.Trim(),
            ScenarioId = Input.ScenarioId,
            TargetParameter = Input.TargetParameter,
            StartValue = Math.Round(Input.StartValue, 2),
            EndValue = Math.Round(Input.EndValue, 2),
            StepValue = Math.Round(Input.StepValue, 2),
            RunCountPerValue = Input.RunCountPerValue,
            AgentCount = Input.AgentCount,
            TotalSteps = Input.TotalSteps,
            LlmProvider = string.IsNullOrWhiteSpace(Input.LlmProvider) ? LlmDefaults.Provider : Input.LlmProvider.Trim(),
            LlmModel = string.IsNullOrWhiteSpace(Input.LlmModel) ? LlmDefaults.MockModel : Input.LlmModel.Trim(),
            Status = ParameterSweepStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        db.ParameterSweeps.Add(sweep);
        await db.SaveChangesAsync();

        return RedirectToPage("/ParameterSweeps/Details", new { id = sweep.Id });
    }

    private async Task LoadScenariosAsync()
    {
        Scenarios = await db.Scenarios
            .OrderBy(item => item.Name)
            .Select(item => new ScenarioOption(item.Id, item.Name))
            .ToListAsync();
    }

    private void ValidateInput()
    {
        if (!BoundaryParameterNames.IsSupported(Input.TargetParameter))
        {
            ModelState.AddModelError("Input.TargetParameter", "対象パラメータが不正です。");
        }

        if (Input.StartValue > Input.EndValue)
        {
            ModelState.AddModelError("Input.EndValue", "終了値は開始値以上である必要があります。");
        }

        if (!Scenarios.Any(item => item.Id == Input.ScenarioId))
        {
            ModelState.AddModelError("Input.ScenarioId", "基準シナリオを選択してください。");
        }
    }

    public sealed record ScenarioOption(int Id, string Name);
}
