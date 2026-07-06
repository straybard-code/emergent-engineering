using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Services;

public sealed class ParameterSweepRunner(AppDbContext db)
{
    public async Task<ParameterSweep?> RecalculateSweepStatusAsync(int sweepId, CancellationToken cancellationToken = default)
    {
        var sweep = await db.ParameterSweeps.FirstOrDefaultAsync(item => item.Id == sweepId, cancellationToken);
        if (sweep is null)
        {
            return null;
        }

        if (sweep.Status is ParameterSweepStatus.Failed or ParameterSweepStatus.Stopped or ParameterSweepStatus.StopRequested or ParameterSweepStatus.Completed)
        {
            return sweep;
        }

        var expectedValues = BuildParameterValues(sweep.StartValue, sweep.EndValue, sweep.StepValue);
        var expectedValueKeys = expectedValues
            .Select(NormalizeParameterValue)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var runs = await db.ParameterSweepRuns
            .Where(item => item.ParameterSweepId == sweepId)
            .Select(item => new { item.ExperimentId, item.ParameterValue })
            .ToListAsync(cancellationToken);

            if (runs.Count == 0)
            {
                if (string.Equals(sweep.Status, ParameterSweepStatus.Running, StringComparison.OrdinalIgnoreCase))
                {
                    return sweep;
                }

                if (!string.Equals(sweep.Status, ParameterSweepStatus.Created, StringComparison.OrdinalIgnoreCase))
                {
                    sweep.Status = ParameterSweepStatus.Created;
                    await db.SaveChangesAsync(cancellationToken);
                }

                return sweep;
            }

        var runKeys = runs
            .Select(item => NormalizeParameterValue(item.ParameterValue))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var experiments = await db.Experiments
            .Where(item => runs.Select(run => run.ExperimentId).Contains(item.Id))
            .Select(item => new { item.Id, item.Status })
            .ToListAsync(cancellationToken);

        var experimentRuns = await db.ExperimentRuns
            .Where(item => experiments.Select(experiment => experiment.Id).Contains(item.ExperimentId))
            .Select(item => new { item.ExperimentId, item.RunNo })
            .ToListAsync(cancellationToken);

        var runCountByExperimentId = experimentRuns
            .GroupBy(item => item.ExperimentId)
            .ToDictionary(group => group.Key, group => group.Count());

        var anyFailed = experiments.Any(item => string.Equals(item.Status, "Failed", StringComparison.OrdinalIgnoreCase));
        var allExpectedValuesProcessed = expectedValueKeys.All(runKeys.Contains);
        var allExperimentsExist = runs.Select(item => item.ExperimentId).Distinct().Count() == expectedValueKeys.Count;
        var allExperimentsCompleted = experiments.Count == expectedValueKeys.Count
            && experiments.All(item => string.Equals(item.Status, ExperimentStatus.Completed, StringComparison.OrdinalIgnoreCase));
        var allRunCountsMet = experiments.Count > 0
            && experiments.All(item => runCountByExperimentId.TryGetValue(item.Id, out var runCount) && runCount >= sweep.RunCountPerValue);

        var computedStatus = anyFailed
            ? ParameterSweepStatus.Failed
            : allExpectedValuesProcessed && allExperimentsExist && allExperimentsCompleted && allRunCountsMet
                ? ParameterSweepStatus.Completed
                : ParameterSweepStatus.Running;

        var needsSave = false;
        if (computedStatus == ParameterSweepStatus.Completed && !sweep.CompletedAt.HasValue)
        {
            sweep.CompletedAt = DateTime.UtcNow;
            needsSave = true;
        }

        if (!string.Equals(sweep.Status, computedStatus, StringComparison.OrdinalIgnoreCase))
        {
            sweep.Status = computedStatus;
            needsSave = true;
        }

        if (needsSave)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return sweep;
    }

    private static List<double> BuildParameterValues(double startValue, double endValue, double stepValue)
    {
        List<double> values = [];
        var start = decimal.Round((decimal)startValue, 3);
        var end = decimal.Round((decimal)endValue, 3);
        var step = decimal.Round((decimal)stepValue, 3);

        for (var value = start; value <= end + 0.0001m; value += step)
        {
            values.Add(Math.Round((double)value, 3));
        }

        return values;
    }

    private static string NormalizeParameterValue(double value)
    {
        return Math.Round(value, 3).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
    }
}
