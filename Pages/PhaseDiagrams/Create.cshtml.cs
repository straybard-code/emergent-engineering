using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.PhaseDiagrams;

public sealed class CreateModel(AppDbContext db, PhaseDiagramRunner phaseDiagramRunner) : PageModel
{
    [BindProperty]
    public PhaseDiagram Input { get; set; } = new()
    {
        Name = "創発相図 実験",
        Description = "2つのパラメータを同時に変化させ、どの領域で Learning / Stable / Emergent / Silo が現れるかを観察します。",
        XParameterName = BoundaryParameterNames.TrustGrowthRate,
        XParameterDisplayName = BoundaryParameterNames.GetLabel(BoundaryParameterNames.TrustGrowthRate),
        XStartValue = 0.2,
        XEndValue = 0.8,
        XStepValue = 0.1,
        YParameterName = BoundaryParameterNames.KnowledgeDiversity,
        YParameterDisplayName = BoundaryParameterNames.GetLabel(BoundaryParameterNames.KnowledgeDiversity),
        YStartValue = 0.3,
        YEndValue = 1.0,
        YStepValue = 0.1,
        RunsPerPoint = 3,
        AgentCount = 10,
        TotalSteps = 50,
        LlmProvider = LlmProviderType.Mock,
        LlmModel = LlmDefaults.MockModel
    };

    public List<ScenarioOption> Scenarios { get; private set; } = [];
    public string AdaptiveNotice { get; private set; } = "";
    public string AdaptiveSourceTypeText { get; private set; } = "--";
    public string AdaptiveReasonText { get; private set; } = "--";
    public int? SourcePhaseDiagramId { get; set; }
    public string? AdaptiveSourceType { get; set; }
    public string? AdaptiveReason { get; set; }

    public int EstimatedGridPointCount => CalculatePointCount(Input.XStartValue, Input.XEndValue, Input.XStepValue)
        * CalculatePointCount(Input.YStartValue, Input.YEndValue, Input.YStepValue);

    public int EstimatedExecutionCount => EstimatedGridPointCount * Math.Max(1, Input.RunsPerPoint);

    public async Task OnGetAsync(int? sourcePhaseDiagramId = null, string? adaptiveSourceType = null, string? adaptiveReason = null)
    {
        await LoadScenariosAsync();

        SourcePhaseDiagramId = sourcePhaseDiagramId;
        AdaptiveSourceType = adaptiveSourceType;
        AdaptiveReason = adaptiveReason;

        if (SourcePhaseDiagramId.HasValue)
        {
            await PrefillFromAdaptiveSourceAsync();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadScenariosAsync();
        NormalizeDisplayNames();
        ValidateInput();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var diagram = new PhaseDiagram
        {
            Name = Input.Name.Trim(),
            Description = Input.Description.Trim(),
            BaseScenarioId = Input.BaseScenarioId,
            XParameterName = Input.XParameterName,
            XParameterDisplayName = BoundaryParameterNames.GetLabel(Input.XParameterName),
            XStartValue = Math.Round(Input.XStartValue, 3),
            XEndValue = Math.Round(Input.XEndValue, 3),
            XStepValue = Math.Round(Input.XStepValue, 3),
            YParameterName = Input.YParameterName,
            YParameterDisplayName = BoundaryParameterNames.GetLabel(Input.YParameterName),
            YStartValue = Math.Round(Input.YStartValue, 3),
            YEndValue = Math.Round(Input.YEndValue, 3),
            YStepValue = Math.Round(Input.YStepValue, 3),
            RunsPerPoint = Input.RunsPerPoint,
            AgentCount = Input.AgentCount,
            TotalSteps = Input.TotalSteps,
            LlmProvider = string.IsNullOrWhiteSpace(Input.LlmProvider) ? LlmDefaults.Provider : Input.LlmProvider.Trim(),
            LlmModel = string.IsNullOrWhiteSpace(Input.LlmModel) ? LlmDefaults.MockModel : Input.LlmModel.Trim(),
            Status = PhaseDiagramStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        db.PhaseDiagrams.Add(diagram);
        await db.SaveChangesAsync();

        return RedirectToPage("/PhaseDiagrams/Details", new { id = diagram.Id });
    }

    private async Task LoadScenariosAsync()
    {
        Scenarios = await db.Scenarios
            .OrderBy(item => item.Name)
            .Select(item => new ScenarioOption(item.Id, item.Name))
            .ToListAsync();
    }

    private async Task PrefillFromAdaptiveSourceAsync()
    {
        if (SourcePhaseDiagramId is null)
        {
            return;
        }

        var parent = await db.PhaseDiagrams.FirstOrDefaultAsync(item => item.Id == SourcePhaseDiagramId.Value);
        if (parent is null)
        {
            AdaptiveNotice = "再探索元の相図が見つからなかったため、通常の新規作成を表示しています。";
            return;
        }

        Input.Name = !string.IsNullOrWhiteSpace(AdaptiveSourceType)
            ? $"{parent.Name} - Adaptive {AdaptiveSourceType}"
            : $"{parent.Name} - Adaptive";
        Input.Description = string.IsNullOrWhiteSpace(AdaptiveReason)
            ? "Adaptive Sweep の初期値が適用されています。必要に応じて調整してください。"
            : AdaptiveReason!;
        Input.BaseScenarioId = parent.BaseScenarioId;
        Input.XParameterName = parent.XParameterName;
        Input.XParameterDisplayName = parent.XParameterDisplayName;
        Input.YParameterName = parent.YParameterName;
        Input.YParameterDisplayName = parent.YParameterDisplayName;
        Input.RunsPerPoint = Math.Max(3, parent.RunsPerPoint);
        Input.AgentCount = parent.AgentCount;
        Input.TotalSteps = parent.TotalSteps;
        Input.LlmProvider = parent.LlmProvider;
        Input.LlmModel = parent.LlmModel;

        try
        {
            var plan = string.IsNullOrWhiteSpace(AdaptiveSourceType)
                ? null
                : await phaseDiagramRunner.BuildAdaptivePlanAsync(parent.Id, AdaptiveSourceType);

            if (plan is not null)
            {
                Input.Name = plan.Name;
                Input.Description = plan.Description;
                Input.XStartValue = plan.XStartValue;
                Input.XEndValue = plan.XEndValue;
                Input.XStepValue = plan.XStepValue;
                Input.YStartValue = plan.YStartValue;
                Input.YEndValue = plan.YEndValue;
                Input.YStepValue = plan.YStepValue;
                AdaptiveSourceTypeText = FormatAdaptiveSourceType(plan.AdaptiveSourceType);
                AdaptiveReasonText = plan.AdaptiveReason;
                AdaptiveNotice = "既存相図から Adaptive Sweep の再探索範囲を読み込んでいます。必要に応じて調整して保存してください。";
                return;
            }
        }
        catch
        {
            // Fall back to a simple copy of the parent settings when the adaptive plan cannot be built.
        }

        Input.Name = !string.IsNullOrWhiteSpace(AdaptiveSourceType)
            ? $"{parent.Name} - Adaptive {AdaptiveSourceType}"
            : $"{parent.Name} - Adaptive";
        Input.Description = string.IsNullOrWhiteSpace(AdaptiveReason)
            ? "Adaptive Sweep の初期値が適用されています。必要に応じて調整してください。"
            : AdaptiveReason!;
        Input.XStartValue = parent.XStartValue;
        Input.XEndValue = parent.XEndValue;
        Input.XStepValue = Math.Max(parent.XStepValue / 2, 0.005);
        Input.YStartValue = parent.YStartValue;
        Input.YEndValue = parent.YEndValue;
        Input.YStepValue = Math.Max(parent.YStepValue / 2, 0.005);

        AdaptiveSourceTypeText = FormatAdaptiveSourceType(AdaptiveSourceType);
        AdaptiveReasonText = string.IsNullOrWhiteSpace(AdaptiveReason) ? "Adaptive Sweep の初期値が適用されています。" : AdaptiveReason!;
        AdaptiveNotice = "既存相図から Adaptive Sweep の初期値を読み込んでいます。必要に応じて範囲を調整して保存してください。";
    }

    private void NormalizeDisplayNames()
    {
        Input.XParameterDisplayName = BoundaryParameterNames.GetLabel(Input.XParameterName);
        Input.YParameterDisplayName = BoundaryParameterNames.GetLabel(Input.YParameterName);
    }

    private void ValidateInput()
    {
        if (!Scenarios.Any(item => item.Id == Input.BaseScenarioId))
        {
            ModelState.AddModelError("Input.BaseScenarioId", "基準シナリオを選択してください。");
        }

        if (!BoundaryParameterNames.IsSupported(Input.XParameterName))
        {
            ModelState.AddModelError("Input.XParameterName", "X軸パラメータが不正です。");
        }

        if (!BoundaryParameterNames.IsSupported(Input.YParameterName))
        {
            ModelState.AddModelError("Input.YParameterName", "Y軸パラメータが不正です。");
        }

        if (string.Equals(Input.XParameterName, Input.YParameterName, StringComparison.Ordinal))
        {
            ModelState.AddModelError("Input.YParameterName", "X軸とY軸には異なるパラメータを選択してください。");
        }

        if (Input.XStartValue > Input.XEndValue)
        {
            ModelState.AddModelError("Input.XEndValue", "X開始値はX終了値以下である必要があります。");
        }

        if (Input.YStartValue > Input.YEndValue)
        {
            ModelState.AddModelError("Input.YEndValue", "Y開始値はY終了値以下である必要があります。");
        }

        if (EstimatedGridPointCount > 100)
        {
            ModelState.AddModelError(string.Empty, $"グリッド点数が多すぎます。現在 {EstimatedGridPointCount} 点です。最大 100 点までにしてください。");
        }
    }

    private static string FormatAdaptiveSourceType(string? adaptiveSourceType) => adaptiveSourceType switch
    {
        "Boundary" => "相境界",
        "HighPipeline" => "高Pipeline",
        "EmergentRegion" => "創発領域",
        "Manual" => "手動",
        _ => string.IsNullOrWhiteSpace(adaptiveSourceType) ? "--" : adaptiveSourceType.Trim()
    };

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
