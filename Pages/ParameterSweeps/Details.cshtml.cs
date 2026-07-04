using System.Text.Json;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Pages.ParameterSweeps;

public sealed class DetailsModel(AppDbContext db) : PageModel
{
    [TempData]
    public string? SweepMessage { get; set; }

    public ParameterSweep? Sweep { get; private set; }
    public string ScenarioName { get; private set; } = "-";
    public List<ParameterSweepRun> Runs { get; private set; } = [];
    public List<ParameterSweepPhaseRow> PhaseComparison { get; private set; } = [];
    public Dictionary<int, string> ExperimentNames { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
        if (Sweep is null)
        {
            return RedirectToPage("/ParameterSweeps/Index");
        }

        ScenarioName = await db.Scenarios
            .Where(item => item.Id == Sweep.ScenarioId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync() ?? "-";

        Runs = await db.ParameterSweepRuns
            .Where(item => item.ParameterSweepId == id)
            .OrderBy(item => item.ParameterValue)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync();

        var experimentIds = Runs
            .Select(item => item.ExperimentId)
            .Distinct()
            .ToList();

        ExperimentNames = experimentIds.Count == 0
            ? []
            : await db.Experiments
                .Where(item => experimentIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name);

        PhaseComparison = Runs
            .Select(item => new ParameterSweepPhaseRow
            {
                ParameterValue = item.ParameterValue,
                Counts = DeserializePhaseSummary(item.FinalPhaseSummaryJson)
            })
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostStopAsync(int id)
    {
        var sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == id);
        if (sweep is null)
        {
            return RedirectToPage("/ParameterSweeps/Index");
        }

        if (sweep.Status == ParameterSweepStatus.Running)
        {
            sweep.Status = ParameterSweepStatus.StopRequested;
            await db.SaveChangesAsync();
            SweepMessage = "停止要求を受け付けました。現在実行中のパラメータ値が終わった後に停止します。";
        }

        return RedirectToPage(new { id });
    }

    public string FormatStatus(string? status) => status switch
    {
        "Created" => "Created（作成済み）",
        "Running" => "Running（実行中）",
        "Completed" => "Completed（完了）",
        "Failed" => "Failed（失敗）",
        "StopRequested" => "StopRequested（停止要求）",
        "Stopped" => "Stopped（停止済み）",
        _ => status ?? "-"
    };

    public string FormatPhaseSummary(string json)
    {
        var summary = DeserializePhaseSummary(json);
        var formatted = string.Join(", ",
            summary
                .Where(item => item.Value > 0)
                .Select(item => $"{item.Key}: {item.Value}"));
        return string.IsNullOrWhiteSpace(formatted) ? "-" : formatted;
    }

    public string GetTrustChartJson()
    {
        var points = Runs.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            averageAbsTrust = item.AverageAbsTrust
        });

        return JsonSerializer.Serialize(points);
    }

    public string GetExperimentName(int experimentId)
    {
        return ExperimentNames.TryGetValue(experimentId, out var name) ? name : $"Experiment #{experimentId}";
    }

    public string GetEmergentChartJson()
    {
        var points = PhaseComparison.Select(item => new
        {
            parameterValue = item.ParameterValue.ToString("0.000"),
            emergentCount = item.GetCount(SimulationPhase.Emergent)
        });

        return JsonSerializer.Serialize(points);
    }

    private static Dictionary<string, int> DeserializePhaseSummary(string json)
    {
        var defaults = new[]
        {
            SimulationPhase.Forming,
            SimulationPhase.Learning,
            SimulationPhase.Stable,
            SimulationPhase.Emergent,
            SimulationPhase.Silo,
            SimulationPhase.Chaos,
            SimulationPhase.Collapse
        }.ToDictionary(item => item, _ => 0);

        if (string.IsNullOrWhiteSpace(json))
        {
            return defaults;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
            foreach (var key in defaults.Keys.ToList())
            {
                defaults[key] = values.GetValueOrDefault(key, 0);
            }
        }
        catch
        {
            return defaults;
        }

        return defaults;
    }
}
