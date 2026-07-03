using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Services;

public sealed class ExperimentExecutionService(AppDbContext db, ISimulationRunner runner) : IExperimentExecutionService
{
    public async Task<Experiment?> RunExperimentAsync(int experimentId, CancellationToken cancellationToken = default)
    {
        var experiment = await db.Experiments.FirstOrDefaultAsync(item => item.Id == experimentId, cancellationToken);
        if (experiment is null)
        {
            return null;
        }

        if (experiment.Status == ExperimentStatus.Running)
        {
            return experiment;
        }

        var existingMaxRunNo = await db.ExperimentRuns
            .Where(run => run.ExperimentId == experimentId)
            .Select(run => (int?)run.RunNo)
            .MaxAsync(cancellationToken) ?? 0;

        experiment.Status = ExperimentStatus.Running;
        await db.SaveChangesAsync(cancellationToken);

        for (var index = 1; index <= experiment.RunCount; index++)
        {
            var latestStatus = await db.Experiments
                .AsNoTracking()
                .Where(item => item.Id == experiment.Id)
                .Select(item => item.Status)
                .FirstAsync(cancellationToken);

            if (latestStatus == ExperimentStatus.StopRequested)
            {
                experiment.Status = ExperimentStatus.Stopped;
                await db.SaveChangesAsync(cancellationToken);
                return experiment;
            }

            var runNo = existingMaxRunNo + index;
            var project = SimulationFactory.CreateProjectFromExperiment(experiment, runNo);
            db.SimulationProjects.Add(project);
            await db.SaveChangesAsync(cancellationToken);

            await runner.RunAllAsync(project.Id, cancellationToken);

            project = await db.SimulationProjects
                .Include(item => item.Metrics)
                .FirstAsync(item => item.Id == project.Id, cancellationToken);
            var metrics = project.Metrics;

            var experimentRun = new ExperimentRun
            {
                ExperimentId = experiment.Id,
                SimulationProjectId = project.Id,
                RunNo = runNo,
                FinalPhase = project.Phase,
                AverageTrust = metrics?.AverageTrust ?? 0,
                NetworkDensity = metrics?.NetworkDensity ?? 0,
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
        }

        experiment.Status = ExperimentStatus.Completed;
        await db.SaveChangesAsync(cancellationToken);

        return experiment;
    }
}
