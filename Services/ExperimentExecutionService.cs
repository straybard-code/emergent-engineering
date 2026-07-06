using System.Text.RegularExpressions;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Services;

public sealed class ExperimentExecutionService(AppDbContext db, ISimulationRunner runner) : IExperimentExecutionService
{
    public Task<Experiment?> RunExperimentAsync(int experimentId, CancellationToken cancellationToken = default)
        => RunExperimentAsync(experimentId, "Summary", cancellationToken);

    public async Task<Experiment?> RunExperimentAsync(int experimentId, string persistenceMode, CancellationToken cancellationToken = default)
    {
        var experiment = await RecalculateExperimentStatusAsync(experimentId, cancellationToken);
        if (experiment is null)
        {
            return null;
        }

        if (string.Equals(experiment.Status, ExperimentStatus.Failed, StringComparison.OrdinalIgnoreCase)
            || string.Equals(experiment.Status, ExperimentStatus.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return experiment;
        }

        var normalizedMode = NormalizePersistenceMode(persistenceMode);
        var projects = await db.SimulationProjects
            .Include(item => item.Metrics)
            .Where(item => item.ExperimentId == experiment.Id)
            .ToListAsync(cancellationToken);
        var runs = await db.ExperimentRuns
            .Where(item => item.ExperimentId == experiment.Id)
            .ToListAsync(cancellationToken);

        var projectsByRunNo = BuildProjectsByRunNo(experiment, projects, runs);
        var runsByRunNo = runs.ToDictionary(item => item.RunNo, item => item);

        experiment.Status = ExperimentStatus.Running;
        await db.SaveChangesAsync(cancellationToken);

        for (var runNo = 1; runNo <= experiment.RunCount; runNo++)
        {
            if (runsByRunNo.TryGetValue(runNo, out var completedRun))
            {
                var completedProject = projects.FirstOrDefault(item => item.Id == completedRun.SimulationProjectId);
                if (completedProject is not null)
                {
                    await NormalizeSimulationProjectCompletionAsync(completedProject, cancellationToken);
                }

                continue;
            }

            if (!projectsByRunNo.TryGetValue(runNo, out var project))
            {
                project = SimulationFactory.CreateProjectFromExperiment(experiment, runNo);
                db.SimulationProjects.Add(project);
                await db.SaveChangesAsync(cancellationToken);
                projects.Add(project);
                projectsByRunNo[runNo] = project;
            }
            else
            {
                await NormalizeSimulationProjectCompletionAsync(project, cancellationToken);
            }

            if (string.Equals(project.Status, SimulationStatus.Failed, StringComparison.OrdinalIgnoreCase))
            {
                experiment.Status = ExperimentStatus.Failed;
                await db.SaveChangesAsync(cancellationToken);
                return experiment;
            }

            if (IsSimulationCompleted(project))
            {
                var existingRun = await EnsureExperimentRunAsync(experiment, project, runNo, cancellationToken);
                if (existingRun is not null)
                {
                    runsByRunNo[runNo] = existingRun;
                }

                continue;
            }

            project.Status = SimulationStatus.Running;
            await db.SaveChangesAsync(cancellationToken);

            await runner.RunAllAsync(project.Id, normalizedMode, cancellationToken);

            project = await db.SimulationProjects
                .Include(item => item.Metrics)
                .FirstAsync(item => item.Id == project.Id, cancellationToken);
            projectsByRunNo[runNo] = project;

            var experimentRun = await EnsureExperimentRunAsync(experiment, project, runNo, cancellationToken);
            if (experimentRun is not null)
            {
                runsByRunNo[runNo] = experimentRun;
            }
        }

        return await RecalculateExperimentStatusAsync(experiment.Id, cancellationToken);
    }

    public async Task<Experiment?> RecalculateExperimentStatusAsync(int experimentId, CancellationToken cancellationToken = default)
    {
        var experiment = await db.Experiments.FirstOrDefaultAsync(item => item.Id == experimentId, cancellationToken);
        if (experiment is null)
        {
            return null;
        }

        var projects = await db.SimulationProjects
            .Include(item => item.Metrics)
            .Where(item => item.ExperimentId == experimentId)
            .ToListAsync(cancellationToken);
        var runs = await db.ExperimentRuns
            .Where(item => item.ExperimentId == experimentId)
            .ToListAsync(cancellationToken);

        var runNoByProjectId = runs
            .GroupBy(item => item.SimulationProjectId)
            .ToDictionary(item => item.Key, item => item.OrderBy(run => run.RunNo).First().RunNo);
        var runsByProjectId = runs
            .GroupBy(item => item.SimulationProjectId)
            .ToDictionary(item => item.Key, item => item.OrderBy(run => run.RunNo).First());

        var hasFailedProject = false;
        var hasIncompleteProject = false;
        var hasChanges = false;

        foreach (var project in projects)
        {
            if (string.Equals(project.Status, SimulationStatus.Failed, StringComparison.OrdinalIgnoreCase))
            {
                hasFailedProject = true;
                continue;
            }

            if (IsSimulationCompleted(project))
            {
                if (!string.Equals(project.Status, SimulationStatus.Completed, StringComparison.OrdinalIgnoreCase))
                {
                    project.Status = SimulationStatus.Completed;
                    hasChanges = true;
                }

                if (project.CurrentStep != project.TotalSteps)
                {
                    project.CurrentStep = project.TotalSteps;
                    hasChanges = true;
                }

                if (!runsByProjectId.TryGetValue(project.Id, out var existingRun))
                {
                    var nextRunNo = runs.Count == 0 ? 1 : runs.Max(item => item.RunNo) + 1;
                    var runNo = TryGetRunNo(project, experiment) ?? nextRunNo;
                    var createdRun = await EnsureExperimentRunAsync(experiment, project, runNo, cancellationToken);
                    runsByProjectId[project.Id] = createdRun;
                    runNoByProjectId[project.Id] = runNo;
                    runs.Add(createdRun);
                    hasChanges = true;
                }

                continue;
            }

            if (!string.Equals(project.Status, SimulationStatus.Running, StringComparison.OrdinalIgnoreCase))
            {
                project.Status = SimulationStatus.Running;
                hasChanges = true;
            }

            hasIncompleteProject = true;
        }

        if (hasChanges)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        if (projects.Count == 0 && runs.Count == 0)
        {
            experiment.Status = ExperimentStatus.Created;
        }
        else if (hasFailedProject)
        {
            experiment.Status = ExperimentStatus.Failed;
        }
        else if (hasIncompleteProject || runs.Count < experiment.RunCount)
        {
            experiment.Status = ExperimentStatus.Running;
        }
        else
        {
            experiment.Status = ExperimentStatus.Completed;
        }

        await db.SaveChangesAsync(cancellationToken);
        return experiment;
    }

    private static string NormalizePersistenceMode(string persistenceMode)
    {
        if (string.Equals(persistenceMode, "Minimal", StringComparison.OrdinalIgnoreCase))
        {
            return "Minimal";
        }

        if (string.Equals(persistenceMode, "Full", StringComparison.OrdinalIgnoreCase))
        {
            return "Full";
        }

        return "Summary";
    }

    private static Dictionary<int, SimulationProject> BuildProjectsByRunNo(
        Experiment experiment,
        IReadOnlyCollection<SimulationProject> projects,
        IReadOnlyCollection<ExperimentRun> runs)
    {
        var runsByProjectId = runs
            .GroupBy(item => item.SimulationProjectId)
            .ToDictionary(item => item.Key, item => item.OrderBy(run => run.RunNo).First());

        var result = new Dictionary<int, SimulationProject>();
        foreach (var project in projects)
        {
            var runNo = TryGetRunNo(project, experiment);
            if (runNo is null && runsByProjectId.TryGetValue(project.Id, out var existingRun))
            {
                runNo = existingRun.RunNo;
            }

            if (runNo is null || runNo <= 0 || result.ContainsKey(runNo.Value))
            {
                continue;
            }

            result[runNo.Value] = project;
        }

        return result;
    }

    private static int? TryGetRunNo(SimulationProject project, Experiment experiment)
    {
        var projectName = project.Name?.Trim();
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return null;
        }

        var experimentName = experiment.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(experimentName))
        {
            var exactPattern = $"^{Regex.Escape(experimentName)}\\s+Run\\s+(\\d+)$";
            var exactMatch = Regex.Match(projectName, exactPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (exactMatch.Success && int.TryParse(exactMatch.Groups[1].Value, out var exactRunNo))
            {
                return exactRunNo;
            }
        }

        var genericMatch = Regex.Match(projectName, @"(?:Run\s+)(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return genericMatch.Success && int.TryParse(genericMatch.Groups[1].Value, out var runNo)
            ? runNo
            : null;
    }

    private static bool IsSimulationCompleted(SimulationProject project)
    {
        return project.CurrentStep >= project.TotalSteps
            || string.Equals(project.Status, SimulationStatus.Completed, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<SimulationProject> NormalizeSimulationProjectCompletionAsync(
        SimulationProject project,
        CancellationToken cancellationToken)
    {
        if (project.CurrentStep < project.TotalSteps
            || string.Equals(project.Status, SimulationStatus.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return project;
        }

        var changed = false;
        if (project.CurrentStep != project.TotalSteps)
        {
            project.CurrentStep = project.TotalSteps;
            changed = true;
        }

        if (!string.Equals(project.Status, SimulationStatus.Completed, StringComparison.OrdinalIgnoreCase))
        {
            project.Status = SimulationStatus.Completed;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return project;
    }

    private async Task<ExperimentRun> EnsureExperimentRunAsync(
        Experiment experiment,
        SimulationProject project,
        int runNo,
        CancellationToken cancellationToken)
    {
        var existingRun = await db.ExperimentRuns.FirstOrDefaultAsync(
            item => item.ExperimentId == experiment.Id && item.RunNo == runNo,
            cancellationToken);

        if (existingRun is null)
        {
            existingRun = await db.ExperimentRuns.FirstOrDefaultAsync(
                item => item.SimulationProjectId == project.Id,
                cancellationToken);
        }

        if (existingRun is not null)
        {
            if (existingRun.SimulationProjectId != project.Id)
            {
                existingRun.SimulationProjectId = project.Id;
            }

            if (existingRun.RunNo != runNo)
            {
                existingRun.RunNo = runNo;
            }

            existingRun.FinalPhase = project.Phase;
            existingRun.AverageTrust = project.Metrics?.AverageTrust ?? existingRun.AverageTrust;
            existingRun.NetworkDensity = project.Metrics?.NetworkDensity ?? existingRun.NetworkDensity;
            existingRun.EffectiveNetworkDensity = project.Metrics?.EffectiveNetworkDensity ?? existingRun.EffectiveNetworkDensity;
            existingRun.StrongLinkCount = project.Metrics?.StrongLinkCount ?? existingRun.StrongLinkCount;
            existingRun.WeakLinkCount = project.Metrics?.WeakLinkCount ?? existingRun.WeakLinkCount;
            existingRun.ComponentCount = project.Metrics?.ComponentCount ?? existingRun.ComponentCount;
            existingRun.IsolatedAgentCount = project.Metrics?.IsolatedAgentCount ?? existingRun.IsolatedAgentCount;
            existingRun.HubAgentName = project.Metrics?.HubAgentName ?? existingRun.HubAgentName;
            existingRun.HubScore = project.Metrics?.HubScore ?? existingRun.HubScore;
            existingRun.ShareInfoRate = project.Metrics?.ShareInfoRate ?? existingRun.ShareInfoRate;
            existingRun.ProposeIdeaRate = project.Metrics?.ProposeIdeaRate ?? existingRun.ProposeIdeaRate;
            existingRun.CriticizeSupportRatio = project.Metrics?.CriticizeSupportRatio ?? existingRun.CriticizeSupportRatio;
            existingRun.StepsToEmergent = project.Metrics?.StepsToEmergent ?? existingRun.StepsToEmergent;
            existingRun.StepsToLearning = project.Metrics?.StepsToLearning ?? existingRun.StepsToLearning;
            existingRun.PhaseChangeCount = project.Metrics?.PhaseChangeCount ?? existingRun.PhaseChangeCount;
            existingRun.PhaseStability = project.Metrics?.PhaseStability ?? existingRun.PhaseStability;
            existingRun.CompletedSteps = project.CurrentStep;
            project.ExperimentRunId = existingRun.Id;
            await db.SaveChangesAsync(cancellationToken);
            return existingRun;
        }

        var metrics = project.Metrics;
        var experimentRun = new ExperimentRun
        {
            ExperimentId = experiment.Id,
            SimulationProjectId = project.Id,
            RunNo = runNo,
            FinalPhase = project.Phase,
            AverageTrust = metrics?.AverageTrust ?? 0,
            NetworkDensity = metrics?.NetworkDensity ?? 0,
            EffectiveNetworkDensity = metrics?.EffectiveNetworkDensity ?? 0,
            StrongLinkCount = metrics?.StrongLinkCount ?? 0,
            WeakLinkCount = metrics?.WeakLinkCount ?? 0,
            ComponentCount = metrics?.ComponentCount ?? 0,
            IsolatedAgentCount = metrics?.IsolatedAgentCount ?? 0,
            HubAgentName = metrics?.HubAgentName ?? "-",
            HubScore = metrics?.HubScore ?? 0,
            ShareInfoRate = metrics?.ShareInfoRate ?? 0,
            ProposeIdeaRate = metrics?.ProposeIdeaRate ?? 0,
            CriticizeSupportRatio = metrics?.CriticizeSupportRatio ?? 0,
            StepsToEmergent = metrics?.StepsToEmergent,
            StepsToLearning = metrics?.StepsToLearning,
            PhaseChangeCount = metrics?.PhaseChangeCount ?? 0,
            PhaseStability = metrics?.PhaseStability ?? 0,
            CompletedSteps = project.CurrentStep,
            CreatedAt = DateTime.UtcNow
        };

        db.ExperimentRuns.Add(experimentRun);
        await db.SaveChangesAsync(cancellationToken);

        project.ExperimentRunId = experimentRun.Id;
        await db.SaveChangesAsync(cancellationToken);
        return experimentRun;
    }
}
