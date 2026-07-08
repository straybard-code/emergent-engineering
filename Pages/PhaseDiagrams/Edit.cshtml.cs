using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class EditModel(AppDbContext db) : PageModel
{
    [TempData]
    public string? DiagramMessage { get; set; }

    [BindProperty]
    public PhaseDiagram Input { get; set; } = new();

    public List<ScenarioOption> Scenarios { get; private set; } = [];
    public PhaseDiagram? Diagram { get; private set; }
    public int ExistingPointCount { get; private set; }
    public bool HasExistingResults => ExistingPointCount > 0;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadScenariosAsync();
        if (!await LoadExistingDiagramAsync(id))
        {
            return RedirectToPage("/PhaseDiagrams/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        await LoadScenariosAsync();
        ValidateInput();

        if (!ModelState.IsValid)
        {
            await LoadExistingDiagramAsync(id, populateInput: false);
            return Page();
        }

        var diagram = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == id);
        if (diagram is null)
        {
            return RedirectToPage("/PhaseDiagrams/Index");
        }

        var existingPoints = await db.PhaseDiagramPoints
            .Where(item => item.PhaseDiagramId == id)
            .Select(item => new { item.ExperimentId })
            .ToListAsync();

        var experimentIds = existingPoints
            .Where(item => item.ExperimentId.HasValue)
            .Select(item => item.ExperimentId!.Value)
            .Distinct()
            .ToList();

        foreach (var experimentId in experimentIds)
        {
            await SimulationDataDeletion.DeleteExperimentAsync(db, experimentId);
        }

        if (existingPoints.Count > 0)
        {
            var points = await db.PhaseDiagramPoints
                .Where(item => item.PhaseDiagramId == id)
                .ToListAsync();

            db.PhaseDiagramPoints.RemoveRange(points);
            await db.SaveChangesAsync();
        }

        diagram.Name = Input.Name.Trim();
        diagram.Description = Input.Description.Trim();
        diagram.BaseScenarioId = Input.BaseScenarioId;
        diagram.XParameterName = Input.XParameterName;
        diagram.XParameterDisplayName = BoundaryParameterNames.GetLabel(Input.XParameterName);
        diagram.XStartValue = Math.Round(Input.XStartValue, 3);
        diagram.XEndValue = Math.Round(Input.XEndValue, 3);
        diagram.XStepValue = Math.Round(Input.XStepValue, 3);
        diagram.YParameterName = Input.YParameterName;
        diagram.YParameterDisplayName = BoundaryParameterNames.GetLabel(Input.YParameterName);
        diagram.YStartValue = Math.Round(Input.YStartValue, 3);
        diagram.YEndValue = Math.Round(Input.YEndValue, 3);
        diagram.YStepValue = Math.Round(Input.YStepValue, 3);
        diagram.RunsPerPoint = Input.RunsPerPoint;
        diagram.AgentCount = Input.AgentCount;
        diagram.TotalSteps = Input.TotalSteps;
        diagram.LlmProvider = string.IsNullOrWhiteSpace(Input.LlmProvider) ? LlmDefaults.Provider : Input.LlmProvider.Trim();
        diagram.LlmModel = string.IsNullOrWhiteSpace(Input.LlmModel) ? LlmDefaults.MockModel : Input.LlmModel.Trim();
        diagram.Status = PhaseDiagramStatus.Pending;
        diagram.CompletedAt = null;

        await db.SaveChangesAsync();
        return RedirectToPage("/PhaseDiagrams/Details", new { id = diagram.Id });
    }

    public async Task<IActionResult> OnPostCopyAsync(int id)
    {
        var source = await db.PhaseDiagrams
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (source is null)
        {
            return RedirectToPage("/PhaseDiagrams/Index");
        }

        var copy = new PhaseDiagram
        {
            Name = await BuildCopyNameAsync(source.Name),
            Description = source.Description,
            BaseScenarioId = source.BaseScenarioId,
            XParameterName = source.XParameterName,
            XParameterDisplayName = source.XParameterDisplayName,
            XStartValue = source.XStartValue,
            XEndValue = source.XEndValue,
            XStepValue = source.XStepValue,
            YParameterName = source.YParameterName,
            YParameterDisplayName = source.YParameterDisplayName,
            YStartValue = source.YStartValue,
            YEndValue = source.YEndValue,
            YStepValue = source.YStepValue,
            RunsPerPoint = source.RunsPerPoint,
            AgentCount = source.AgentCount,
            TotalSteps = source.TotalSteps,
            ParentPhaseDiagramId = source.Id,
            AdaptiveSourceType = source.AdaptiveSourceType,
            AdaptiveReason = source.AdaptiveReason,
            IsAdaptiveSweep = source.IsAdaptiveSweep,
            LlmProvider = source.LlmProvider,
            LlmModel = source.LlmModel,
            Status = PhaseDiagramStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = null
        };

        db.PhaseDiagrams.Add(copy);
        await db.SaveChangesAsync();

        DiagramMessage = "相図をコピーして新規作成しました。";
        return RedirectToPage("/PhaseDiagrams/Edit", new { id = copy.Id });
    }

    private async Task LoadScenariosAsync()
    {
        Scenarios = await db.Scenarios
            .OrderBy(item => item.Name)
            .Select(item => new ScenarioOption(item.Id, item.Name))
            .ToListAsync();
    }

    private async Task<bool> LoadExistingDiagramAsync(int id, bool populateInput = true)
    {
        Diagram = await db.PhaseDiagrams
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (Diagram is null)
        {
            return false;
        }

        ExistingPointCount = await db.PhaseDiagramPoints.CountAsync(item => item.PhaseDiagramId == id);
        if (populateInput)
        {
            Input = Diagram;
        }

        return true;
    }

    private void ValidateInput()
    {
        if (!Scenarios.Any(item => item.Id == Input.BaseScenarioId))
        {
            ModelState.AddModelError("Input.BaseScenarioId", "対象シナリオを選択してください。");
        }

        if (!BoundaryParameterNames.IsSupported(Input.XParameterName))
        {
            ModelState.AddModelError("Input.XParameterName", "X軸パラメータが正しくありません。");
        }

        if (!BoundaryParameterNames.IsSupported(Input.YParameterName))
        {
            ModelState.AddModelError("Input.YParameterName", "Y軸パラメータが正しくありません。");
        }

        if (string.Equals(Input.XParameterName, Input.YParameterName, StringComparison.Ordinal))
        {
            ModelState.AddModelError("Input.YParameterName", "X軸とY軸には異なるパラメータを選択してください。");
        }

        if (Input.XStartValue >= Input.XEndValue)
        {
            ModelState.AddModelError("Input.XEndValue", "X開始値はX終了値より小さくしてください。");
        }

        if (Input.YStartValue >= Input.YEndValue)
        {
            ModelState.AddModelError("Input.YEndValue", "Y開始値はY終了値より小さくしてください。");
        }

        var gridPoints = CalculatePointCount(Input.XStartValue, Input.XEndValue, Input.XStepValue)
            * CalculatePointCount(Input.YStartValue, Input.YEndValue, Input.YStepValue);
        if (gridPoints > 100)
        {
            ModelState.AddModelError(string.Empty, $"グリッド点数が多すぎます。現在 {gridPoints} 点です。最大 100 点までにしてください。");
        }
    }

    private async Task<string> BuildCopyNameAsync(string sourceName)
    {
        var baseName = string.IsNullOrWhiteSpace(sourceName) ? "相図" : sourceName.Trim();
        var candidate = $"{baseName} のコピー";
        var existingNames = await db.PhaseDiagrams
            .AsNoTracking()
            .Select(item => item.Name)
            .ToListAsync();

        var names = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        if (!names.Contains(candidate))
        {
            return candidate;
        }

        var index = 2;
        while (names.Contains($"{candidate} {index}"))
        {
            index++;
        }

        return $"{candidate} {index}";
    }

    private static int CalculatePointCount(double startValue, double endValue, double stepValue)
    {
        if (stepValue <= 0)
        {
            return 0;
        }

        var start = decimal.Round((decimal)startValue, 3);
        var end = decimal.Round((decimal)endValue, 3);
        var step = decimal.Round((decimal)stepValue, 3);
        var count = 0;

        for (var value = start; value <= end + 0.0001m; value += step)
        {
            count++;
        }

        return count;
    }

    public sealed record ScenarioOption(int Id, string Name);
}
