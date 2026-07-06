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

        var deletableExperimentIds = matchedExperiments
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

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var simulationProjectIds = await db.SimulationProjects
                .Where(project => project.ExperimentId.HasValue && deletableExperimentIds.Contains(project.ExperimentId.Value))
                .Select(project => project.Id)
                .ToListAsync(cancellationToken);

            var affectedSweepIds = await db.ParameterSweepRuns
                .Where(run => deletableExperimentIds.Contains(run.ExperimentId))
                .Select(run => run.ParameterSweepId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var result = new ExperimentCleanupResult
            {
                DeletedExperiments = deletableExperimentIds.Count,
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
                    run => deletableExperimentIds.Contains(run.ExperimentId)
                        || simulationProjectIds.Contains(run.SimulationProjectId),
                    cancellationToken),
                DeletedParameterSweepRuns = await db.ParameterSweepRuns.CountAsync(
                    run => deletableExperimentIds.Contains(run.ExperimentId),
                    cancellationToken),
                SkippedRunningExperiments = skippedRunningExperiments
            };

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
                    .Where(run => deletableExperimentIds.Contains(run.ExperimentId)
                        || simulationProjectIds.Contains(run.SimulationProjectId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (result.DeletedParameterSweepRuns > 0)
            {
                await db.ParameterSweepRuns
                    .Where(run => deletableExperimentIds.Contains(run.ExperimentId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (result.DeletedSimulationProjects > 0)
            {
                await db.SimulationProjects
                    .Where(project => project.ExperimentId.HasValue && deletableExperimentIds.Contains(project.ExperimentId.Value))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await db.Experiments
                .Where(item => deletableExperimentIds.Contains(item.Id))
                .ExecuteDeleteAsync(cancellationToken);

            if (recalculateSweepStatusAsync is not null && affectedSweepIds.Count > 0)
            {
                foreach (var sweepId in affectedSweepIds)
                {
                    await recalculateSweepStatusAsync(sweepId, cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsDeleteBlocked(string? status)
    {
        return string.Equals(status, ExperimentStatus.Running, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, ExperimentStatus.StopRequested, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ExperimentCleanupResult
{
    public int DeletedExperiments { get; init; }
    public int DeletedSimulationProjects { get; init; }
    public int DeletedSimulationSteps { get; init; }
    public int DeletedAgentActions { get; init; }
    public int DeletedTrustSnapshots { get; init; }
    public int DeletedSimulationMetrics { get; init; }
    public int DeletedAgents { get; init; }
    public int DeletedExperimentRuns { get; init; }
    public int DeletedParameterSweepRuns { get; init; }
    public int SkippedRunningExperiments { get; init; }

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
