using EmergentEngineering.Data;
using EmergentEngineering.Models;
using EmergentEngineering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EmergentEngineering.Pages.Scenarios;

public sealed class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public Scenario Input { get; set; } = new()
    {
        Name = "協調型シナリオ",
        Description = "情報共有と学習を重視する基本シナリオ",
        Purpose = "顧客学習を進めながら協調的な組織形成パターンを観察する",
        BoundaryConditions = "情報共有を重視する。困ったときは相談してよい。失敗は学習材料として扱い、互いに支援する。",
        KpiDefinition = "市場学習、品質、リピート率、顧客成功",
        AgentCount = 4,
        TotalSteps = 5,
        RunCount = 3,
        LlmProvider = LlmProviderType.Mock,
        LlmModel = LlmDefaults.MockModel,
        InformationSharingLevel = BoundaryParameterDefaults.Level,
        CooperationLevel = BoundaryParameterDefaults.Level,
        CompetitionLevel = BoundaryParameterDefaults.Level,
        PsychologicalSafetyLevel = BoundaryParameterDefaults.Level,
        LearningOrientationLevel = BoundaryParameterDefaults.Level,
        CustomerOrientationLevel = BoundaryParameterDefaults.Level,
        ShortTermResultPressureLevel = BoundaryParameterDefaults.Level,
        EffectiveTrustThreshold = BoundaryParameterDefaults.EffectiveTrustThreshold,
        KnowledgeStock = KnowledgeDefaults.Stock,
        KnowledgeDiversity = KnowledgeDefaults.Diversity,
        ExternalShockLevel = KnowledgeDefaults.ExternalShockLevel,
        CrossDomainExposure = KnowledgeDefaults.CrossDomainExposure,
        RewiringSensitivity = KnowledgeDefaults.RewiringSensitivity,
        EnableExternalShock = KnowledgeDefaults.EnableExternalShock,
        ShockStep = KnowledgeDefaults.ShockStep,
        ShockType = KnowledgeDefaults.ShockType,
        ShockDescription = KnowledgeDefaults.ShockDescription,
        EnableChallengeEvent = ChallengeDefaults.EnableChallengeEvent,
        ChallengeType = ChallengeDefaults.ChallengeType,
        ChallengeStep = ChallengeDefaults.ChallengeStep,
        ChallengeLevel = ChallengeDefaults.ChallengeLevel,
        ChallengeDescription = ChallengeDefaults.ChallengeDescription,
        RequiredKnowledgeDiversity = ChallengeDefaults.RequiredKnowledgeDiversity,
        RequiredCrossDomainExposure = ChallengeDefaults.RequiredCrossDomainExposure,
        RequiredRewiringScore = ChallengeDefaults.RequiredRewiringScore
    };

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var scenario = SimulationFactory.CreateScenario(Input);
        db.Scenarios.Add(scenario);
        await db.SaveChangesAsync();

        return RedirectToPage("/Scenarios/Details", new { id = scenario.Id });
    }
}
