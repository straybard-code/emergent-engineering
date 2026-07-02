using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EmergentEngineering.Pages.Simulations;

public sealed class DetailsModel(AppDbContext db, ISimulationRunner runner) : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string SelectedView { get; private set; } = "logs";
    public SimulationProject? Project { get; private set; }
    public List<AgentAction> Actions { get; private set; } = [];
    public List<AgentAction> AgentHistory { get; private set; } = [];
    public List<PhasePoint> PhaseHistory { get; private set; } = [];
    public List<AgentTrustSummary> AgentTrustSummaries { get; private set; } = [];
    public int? SelectedAgentId { get; private set; }
    public bool IsCompleted => Project is not null && Project.CurrentStep >= Project.TotalSteps;

    public async Task<IActionResult> OnGetAsync(int id, int? agentId, string? view)
    {
        SelectedAgentId = agentId;
        SelectedView = string.Equals(view, "history", StringComparison.OrdinalIgnoreCase) ? "history" : "logs";
        await LoadAsync(id, agentId);
        return Page();
    }

    public async Task<IActionResult> OnPostRunOneAsync(int id)
    {
        await runner.RunOneStepAsync(id);
        return RedirectToPage(new { id, view = "logs" });
    }

    public async Task<IActionResult> OnPostRunAllAsync(int id)
    {
        await runner.RunAllAsync(id);
        return RedirectToPage(new { id, view = "logs" });
    }

    private async Task LoadAsync(int id, int? agentId)
    {
        Project = await db.SimulationProjects
            .Include(project => project.Agents)
            .FirstOrDefaultAsync(project => project.Id == id);

        if (Project is null)
        {
            return;
        }

        Actions = await db.AgentActions
            .Include(action => action.Agent)
            .Where(action => action.SimulationProjectId == id)
            .OrderByDescending(action => action.StepNo)
            .ThenBy(action => action.AgentId)
            .ToListAsync();

        PhaseHistory = await db.SimulationSteps
            .Where(step => step.SimulationProjectId == id)
            .OrderBy(step => step.StepNo)
            .Select(step => new PhasePoint
            {
                StepNo = step.StepNo,
                Phase = step.Phase,
                PhaseValue = MapPhase(step.Phase)
            })
            .ToListAsync();

        AgentTrustSummaries = Project.Agents
            .OrderBy(agent => agent.Id)
            .Select(agent => new AgentTrustSummary
            {
                AgentName = agent.Name,
                AverageTrust = CalculateAverageTrust(agent.TrustJson),
                TrustJson = agent.TrustJson
            })
            .ToList();

        if (agentId.HasValue)
        {
            Actions = Actions
                .Where(action => action.AgentId == agentId.Value)
                .ToList();

            AgentHistory = Actions
                .Where(action => action.AgentId == agentId.Value)
                .OrderBy(action => action.StepNo)
                .ToList();
        }
    }

    public string GetActionCssClass(string action)
    {
        return action switch
        {
            AgentActionType.ShareInfo => "action-share",
            AgentActionType.AskHelp => "action-help",
            AgentActionType.ProposeIdea => "action-propose",
            AgentActionType.Criticize => "action-criticize",
            AgentActionType.WorkAlone => "action-alone",
            AgentActionType.SupportOther => "action-support",
            _ => "action-wait"
        };
    }

    public string GetPhaseHistoryJson()
    {
        return JsonSerializer.Serialize(PhaseHistory, JsonOptions);
    }

    private static int MapPhase(string phase)
    {
        return phase switch
        {
            SimulationPhase.Forming => 0,
            SimulationPhase.Learning => 1,
            SimulationPhase.Stable => 2,
            SimulationPhase.Emergent => 3,
            SimulationPhase.Silo => -1,
            SimulationPhase.Chaos => -2,
            SimulationPhase.Collapse => -3,
            _ => 0
        };
    }

    private static double CalculateAverageTrust(string trustJson)
    {
        if (string.IsNullOrWhiteSpace(trustJson))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(trustJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return 0;
            }

            List<double> values = [];
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (TryReadTrustValue(property.Value, out var value))
                {
                    values.Add(value);
                }
            }

            return values.Count == 0 ? 0 : Math.Round(values.Average(), 2);
        }
        catch
        {
            return 0;
        }
    }

    private static bool TryReadTrustValue(JsonElement element, out double value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.TryGetDouble(out value);

            case JsonValueKind.String:
                return double.TryParse(element.GetString(), out value);

            default:
                value = 0;
                return false;
        }
    }
}
