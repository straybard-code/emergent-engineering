using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Services;

public sealed class ExperimentCleanupService(AppDbContext db)
{
    public Task<ExperimentCleanupResult> DeleteExperimentsAsync(
        IReadOnlyCollection<int> experimentIds,
        CancellationToken cancellationToken = default)
        => DeleteExperimentsAsync(experimentIds, null, cancellationToken);

    public async Task<ExperimentCleanupResult> DeleteExperimentsAsync(
        IReadOnlyCollection<int> experimentIds,
        Func<int, CancellationToken, Task>? recalculateSweepStatusAsync,
        CancellationToken cancellationToken = default)
        => await DeleteExperimentsCoreAsync(
            experimentIds,
            recalculateSweepStatusAsync,
            forceDelete: false,
            cancellationToken);

    public Task<ExperimentCleanupResult> ForceDeleteExperimentsAsync(
        IReadOnlyCollection<int> experimentIds,
        Func<int, CancellationToken, Task>? recalculateSweepStatusAsync = null,
        CancellationToken cancellationToken = default)
        => DeleteExperimentsCoreAsync(experimentIds, recalculateSweepStatusAsync, forceDelete: true, cancellationToken);

    public Task<ExperimentCleanupResult> ForceDeleteExperimentAsync(
        int experimentId,
        Func<int, CancellationToken, Task>? recalculateSweepStatusAsync = null,
        CancellationToken cancellationToken = default)
        => ForceDeleteExperimentsAsync([experimentId], recalculateSweepStatusAsync, cancellationToken);

    public async Task<bool> MarkExperimentAsFailedAsync(int experimentId, CancellationToken cancellationToken = default)
    {
        var experiment = await db.Experiments
            .FirstOrDefaultAsync(item => item.Id == experimentId, cancellationToken);

        if (experiment is null)
        {
            return false;
        }

        experiment.Status = ExperimentStatus.Failed;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkSimulationAsFailedAsync(int simulationProjectId, CancellationToken cancellationToken = default)
    {
        var project = await db.SimulationProjects
            .FirstOrDefaultAsync(item => item.Id == simulationProjectId, cancellationToken);

        if (project is null)
        {
            return false;
        }

        project.Status = SimulationStatus.Failed;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkParameterSweepAsFailedAsync(int parameterSweepId, CancellationToken cancellationToken = default)
    {
        var sweep = await db.ParameterSweeps
            .FirstOrDefaultAsync(item => item.Id == parameterSweepId, cancellationToken);

        if (sweep is null)
        {
            return false;
        }

        sweep.Status = ParameterSweepStatus.Failed;
        sweep.CompletedAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<ExperimentCleanupResult> ForceDeleteSimulationAsync(
        int simulationProjectId,
        Func<int, CancellationToken, Task>? recalculateSweepStatusAsync = null,
        CancellationToken cancellationToken = default)
        => DeleteSimulationAsync(simulationProjectId, recalculateSweepStatusAsync, cancellationToken);

    public Task<ExperimentCleanupResult> ForceDeleteParameterSweepAsync(
        int parameterSweepId,
        CancellationToken cancellationToken = default)
        => DeleteParameterSweepAsync(parameterSweepId, cancellationToken);

    private async Task<ExperimentCleanupResult> DeleteExperimentsCoreAsync(
        IReadOnlyCollection<int> experimentIds,
        Func<int, CancellationToken, Task>? recalculateSweepStatusAsync,
        bool forceDelete,
        CancellationToken cancellationToken)
    {
        var targetExperimentIds = experimentIds
            .Where(item => item > 0)
            .Distinct()
            .ToList();

        if (targetExperimentIds.Count == 0)
        {
            return new ExperimentCleanupResult();
        }

        var matchedExperiments = await db.Experiments
            .Where(item => targetExperimentIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Status })
            .ToListAsync(cancellationToken);

        var deletableExperimentIds = forceDelete
            ? matchedExperiments.Select(item => item.Id).ToList()
            : matchedExperiments
                .Where(item => !IsDeleteBlocked(item.Status))
                .Select(item => item.Id)
                .ToList();

        var skippedRunningExperiments = matchedExperiments.Count - deletableExperimentIds.Count;
        if (deletableExperimentIds.Count == 0)
        {
            return new ExperimentCleanupResult
            {
                SkippedRunningExperiments = skippedRunningExperiments
            };
        }

        var result = new ExperimentCleanupResult
        {
            SkippedRunningExperiments = skippedRunningExperiments
        };
        var affectedSweepIds = new HashSet<int>();

        foreach (var experimentId in deletableExperimentIds)
        {
            try
            {
                var cleanup = await DeleteSingleExperimentAsync(experimentId, cancellationToken);
                result.Add(cleanup);

                foreach (var sweepId in cleanup.AffectedSweepIds)
                {
                    affectedSweepIds.Add(sweepId);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Experiment {experimentId}: {ex.Message}");
                if (!forceDelete)
                {
                    throw;
                }
            }
        }

        if (recalculateSweepStatusAsync is not null && affectedSweepIds.Count > 0)
        {
            foreach (var sweepId in affectedSweepIds)
            {
                await recalculateSweepStatusAsync(sweepId, cancellationToken);
            }
        }

        return result;
    }

    private async Task<ExperimentCleanupResult> DeleteSimulationAsync(
        int simulationProjectId,
        Func<int, CancellationToken, Task>? recalculateSweepStatusAsync,
        CancellationToken cancellationToken)
    {
        var project = await db.SimulationProjects
            .Where(item => item.Id == simulationProjectId)
            .Select(item => new { item.Id, item.ExperimentId })
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return new ExperimentCleanupResult();
        }

        List<int> experimentIds = project.ExperimentId.HasValue
            ? new List<int> { project.ExperimentId.Value }
            : new List<int>();

        List<int> affectedSweepIds = project.ExperimentId.HasValue
            ? await db.ParameterSweepRuns
                .Where(run => run.ExperimentId == project.ExperimentId.Value)
                .Select(run => run.ParameterSweepId)
                .Distinct()
                .ToListAsync(cancellationToken)
            : new List<int>();

        var result = new ExperimentCleanupResult();
        var childSimulationProjectIds = new[] { simulationProjectId };

        result.DeletedSimulationSteps = await db.SimulationSteps.CountAsync(step => childSimulationProjectIds.Contains(step.SimulationProjectId), cancellationToken);
        result.DeletedAgentActions = await db.AgentActions.CountAsync(action => childSimulationProjectIds.Contains(action.SimulationProjectId), cancellationToken);
        result.DeletedTrustSnapshots = await db.TrustSnapshots.CountAsync(snapshot => childSimulationProjectIds.Contains(snapshot.SimulationProjectId), cancellationToken);
        result.DeletedSimulationMetrics = await db.SimulationMetrics.CountAsync(metrics => childSimulationProjectIds.Contains(metrics.SimulationProjectId), cancellationToken);
        result.DeletedAgents = await db.Agents.CountAsync(agent => childSimulationProjectIds.Contains(agent.SimulationProjectId), cancellationToken);
        result.DeletedExperimentRuns = await db.ExperimentRuns.CountAsync(run => childSimulationProjectIds.Contains(run.SimulationProjectId), cancellationToken);
        result.DeletedParameterSweepRuns = project.ExperimentId.HasValue
            ? await db.ParameterSweepRuns.CountAsync(run => run.ExperimentId == project.ExperimentId.Value, cancellationToken)
            : 0;

        if (result.DeletedAgentActions > 0)
        {
            await db.AgentActions
                .Where(action => childSimulationProjectIds.Contains(action.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedTrustSnapshots > 0)
        {
            await db.TrustSnapshots
                .Where(snapshot => childSimulationProjectIds.Contains(snapshot.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedSimulationSteps > 0)
        {
            await db.SimulationSteps
                .Where(step => childSimulationProjectIds.Contains(step.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedSimulationMetrics > 0)
        {
            await db.SimulationMetrics
                .Where(metrics => childSimulationProjectIds.Contains(metrics.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedAgents > 0)
        {
            await db.Agents
                .Where(agent => childSimulationProjectIds.Contains(agent.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedExperimentRuns > 0)
        {
            await db.ExperimentRuns
                .Where(run => childSimulationProjectIds.Contains(run.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedParameterSweepRuns > 0 && project.ExperimentId.HasValue)
        {
            await db.ParameterSweepRuns
                .Where(run => run.ExperimentId == project.ExperimentId.Value)
                .ExecuteDeleteAsync(cancellationToken);
        }

        await db.SimulationProjects
            .Where(item => item.Id == simulationProjectId)
            .ExecuteDeleteAsync(cancellationToken);

        return result;
    }

    private async Task<ExperimentCleanupResult> DeleteParameterSweepAsync(
        int parameterSweepId,
        CancellationToken cancellationToken)
    {
        var sweep = await db.ParameterSweeps
            .Where(item => item.Id == parameterSweepId)
            .Select(item => new { item.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (sweep is null)
        {
            return new ExperimentCleanupResult();
        }

        var result = new ExperimentCleanupResult
        {
            DeletedParameterSweeps = 1,
            DeletedParameterSweepRuns = await db.ParameterSweepRuns.CountAsync(run => run.ParameterSweepId == parameterSweepId, cancellationToken)
        };

        if (result.DeletedParameterSweepRuns > 0)
        {
            await db.ParameterSweepRuns
                .Where(run => run.ParameterSweepId == parameterSweepId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        await db.ParameterSweeps
            .Where(item => item.Id == parameterSweepId)
            .ExecuteDeleteAsync(cancellationToken);

        return result;
    }

    private async Task<ExperimentCleanupResult> DeleteSingleExperimentAsync(int experimentId, CancellationToken cancellationToken)
    {
        var experiment = await db.Experiments
            .Where(item => item.Id == experimentId)
            .Select(item => new { item.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (experiment is null)
        {
            return new ExperimentCleanupResult();
        }

        var simulationProjectIds = await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .Select(project => project.Id)
            .ToListAsync(cancellationToken);

        var phaseDiagramPointCount = await db.PhaseDiagramPoints.CountAsync(
            point => point.ExperimentId == experimentId,
            cancellationToken);

        var affectedSweepIds = await db.ParameterSweepRuns
            .Where(run => run.ExperimentId == experimentId)
            .Select(run => run.ParameterSweepId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var result = new ExperimentCleanupResult
        {
            DeletedExperiments = 1,
            DeletedSimulationProjects = simulationProjectIds.Count,
            DeletedSimulationSteps = simulationProjectIds.Count == 0
                ? 0
                : await db.SimulationSteps.CountAsync(step => simulationProjectIds.Contains(step.SimulationProjectId), cancellationToken),
            DeletedAgentActions = simulationProjectIds.Count == 0
                ? 0
                : await db.AgentActions.CountAsync(action => simulationProjectIds.Contains(action.SimulationProjectId), cancellationToken),
            DeletedTrustSnapshots = simulationProjectIds.Count == 0
                ? 0
                : await db.TrustSnapshots.CountAsync(snapshot => simulationProjectIds.Contains(snapshot.SimulationProjectId), cancellationToken),
            DeletedSimulationMetrics = simulationProjectIds.Count == 0
                ? 0
                : await db.SimulationMetrics.CountAsync(metrics => simulationProjectIds.Contains(metrics.SimulationProjectId), cancellationToken),
            DeletedAgents = simulationProjectIds.Count == 0
                ? 0
                : await db.Agents.CountAsync(agent => simulationProjectIds.Contains(agent.SimulationProjectId), cancellationToken),
            DeletedExperimentRuns = await db.ExperimentRuns.CountAsync(
                run => run.ExperimentId == experimentId || simulationProjectIds.Contains(run.SimulationProjectId),
                cancellationToken),
            DeletedParameterSweepRuns = await db.ParameterSweepRuns.CountAsync(
                run => run.ExperimentId == experimentId,
                cancellationToken),
            DeletedPhaseDiagramPoints = phaseDiagramPointCount,
            AffectedSweepIds = affectedSweepIds
        };

        if (phaseDiagramPointCount > 0)
        {
            await db.PhaseDiagramPoints
                .Where(point => point.ExperimentId == experimentId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedAgentActions > 0)
        {
            await db.AgentActions
                .Where(action => simulationProjectIds.Contains(action.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedTrustSnapshots > 0)
        {
            await db.TrustSnapshots
                .Where(snapshot => simulationProjectIds.Contains(snapshot.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedSimulationSteps > 0)
        {
            await db.SimulationSteps
                .Where(step => simulationProjectIds.Contains(step.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedSimulationMetrics > 0)
        {
            await db.SimulationMetrics
                .Where(metrics => simulationProjectIds.Contains(metrics.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedAgents > 0)
        {
            await db.Agents
                .Where(agent => simulationProjectIds.Contains(agent.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedExperimentRuns > 0)
        {
            await db.ExperimentRuns
                .Where(run => run.ExperimentId == experimentId || simulationProjectIds.Contains(run.SimulationProjectId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (result.DeletedParameterSweepRuns > 0)
        {
            await db.ParameterSweepRuns
                .Where(run => run.ExperimentId == experimentId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        await db.SimulationProjects
            .Where(project => project.ExperimentId == experimentId)
            .ExecuteDeleteAsync(cancellationToken);

        await db.Experiments
            .Where(item => item.Id == experimentId)
            .ExecuteDeleteAsync(cancellationToken);

        return result;
    }

    private static bool IsDeleteBlocked(string? status)
    {
        return string.Equals(status, ExperimentStatus.Running, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ExperimentStatus.StopRequested, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ExperimentCleanupResult
{
    public int DeletedExperiments { get; set; }
    public int DeletedSimulationProjects { get; set; }
    public int DeletedSimulationSteps { get; set; }
    public int DeletedAgentActions { get; set; }
    public int DeletedTrustSnapshots { get; set; }
    public int DeletedSimulationMetrics { get; set; }
    public int DeletedAgents { get; set; }
    public int DeletedExperimentRuns { get; set; }
    public int DeletedParameterSweepRuns { get; set; }
    public int DeletedParameterSweeps { get; set; }
    public int DeletedPhaseDiagramPoints { get; set; }
    public int SkippedRunningExperiments { get; set; }
    public List<int> AffectedSweepIds { get; set; } = [];
    public List<string> Errors { get; set; } = [];

    public void Add(ExperimentCleanupResult other)
    {
        DeletedExperiments += other.DeletedExperiments;
        DeletedSimulationProjects += other.DeletedSimulationProjects;
        DeletedSimulationSteps += other.DeletedSimulationSteps;
        DeletedAgentActions += other.DeletedAgentActions;
        DeletedTrustSnapshots += other.DeletedTrustSnapshots;
        DeletedSimulationMetrics += other.DeletedSimulationMetrics;
        DeletedAgents += other.DeletedAgents;
        DeletedExperimentRuns += other.DeletedExperimentRuns;
        DeletedParameterSweepRuns += other.DeletedParameterSweepRuns;
        DeletedParameterSweeps += other.DeletedParameterSweeps;
        DeletedPhaseDiagramPoints += other.DeletedPhaseDiagramPoints;
        SkippedRunningExperiments += other.SkippedRunningExperiments;
        AffectedSweepIds.AddRange(other.AffectedSweepIds);
        Errors.AddRange(other.Errors);
    }

    public string ToJapaneseMessage()
    {
        List<string> parts = [];
        AppendPart(parts, "Experiment", DeletedExperiments);
        AppendPart(parts, "Simulation", DeletedSimulationProjects);
        AppendPart(parts, "Agent", DeletedAgents);
        AppendPart(parts, "Step", DeletedSimulationSteps);
        AppendPart(parts, "Action", DeletedAgentActions);
        AppendPart(parts, "TrustSnapshot", DeletedTrustSnapshots);
        AppendPart(parts, "Metric", DeletedSimulationMetrics);
        AppendPart(parts, "Run", DeletedExperimentRuns);
        AppendPart(parts, "ParameterSweep結果", DeletedParameterSweepRuns);
        AppendPart(parts, "ParameterSweep", DeletedParameterSweeps);
        AppendPart(parts, "PhaseDiagramPoint", DeletedPhaseDiagramPoints);

        if (parts.Count == 0)
        {
            return SkippedRunningExperiments > 0
                ? $"実行中のExperiment {SkippedRunningExperiments}件は削除できないため除外しました。"
                : "削除対象のExperimentがありませんでした。";
        }

        var message = $"{string.Join("、", parts)}を削除しました。";
        if (SkippedRunningExperiments > 0)
        {
            message += $" 実行中のExperiment {SkippedRunningExperiments}件は削除できないため除外しました。";
        }

        return message;
    }

    private static void AppendPart(ICollection<string> parts, string label, int count)
    {
        if (count > 0)
        {
            parts.Add($"{label} {count}件");
        }
    }
}
