using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ParameterSweep> ParameterSweeps => Set<ParameterSweep>();
    public DbSet<ParameterSweepRun> ParameterSweepRuns => Set<ParameterSweepRun>();
    public DbSet<PhaseDiagram> PhaseDiagrams => Set<PhaseDiagram>();
    public DbSet<PhaseDiagramPoint> PhaseDiagramPoints => Set<PhaseDiagramPoint>();
    public DbSet<Experiment> Experiments => Set<Experiment>();
    public DbSet<ExperimentRun> ExperimentRuns => Set<ExperimentRun>();
    public DbSet<SimulationMetrics> SimulationMetrics => Set<SimulationMetrics>();
    public DbSet<SimulationProject> SimulationProjects => Set<SimulationProject>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<SimulationStep> SimulationSteps => Set<SimulationStep>();
    public DbSet<AgentAction> AgentActions => Set<AgentAction>();
    public DbSet<TrustSnapshot> TrustSnapshots => Set<TrustSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimulationProject>(entity =>
        {
            entity.Property(project => project.Name).HasMaxLength(200);
            entity.Property(project => project.LlmProvider).HasMaxLength(40);
            entity.Property(project => project.LlmModel).HasMaxLength(120);
            entity.Property(project => project.Status).HasMaxLength(40);
            entity.Property(project => project.Phase).HasMaxLength(40);
            entity.Property(project => project.EffectiveTrustThreshold).HasDefaultValue(BoundaryParameterDefaults.EffectiveTrustThreshold);
            entity.Property(project => project.KnowledgeStock).HasDefaultValue(KnowledgeDefaults.Stock);
            entity.Property(project => project.KnowledgeDiversity).HasDefaultValue(KnowledgeDefaults.Diversity);
            entity.Property(project => project.ExternalShockLevel).HasDefaultValue(KnowledgeDefaults.ExternalShockLevel);
            entity.Property(project => project.CrossDomainExposure).HasDefaultValue(KnowledgeDefaults.CrossDomainExposure);
            entity.Property(project => project.RewiringSensitivity).HasDefaultValue(KnowledgeDefaults.RewiringSensitivity);
            entity.Property(project => project.EnableExternalShock).HasDefaultValue(KnowledgeDefaults.EnableExternalShock);
            entity.Property(project => project.ShockStep).HasDefaultValue(KnowledgeDefaults.ShockStep);
            entity.Property(project => project.ShockType).HasMaxLength(40).HasDefaultValue(KnowledgeDefaults.ShockType);
            entity.Property(project => project.ShockDescription).HasDefaultValue(KnowledgeDefaults.ShockDescription);
            entity.Property(project => project.EnableChallengeEvent).HasDefaultValue(ChallengeDefaults.EnableChallengeEvent);
            entity.Property(project => project.ChallengeType).HasMaxLength(40).HasDefaultValue(ChallengeDefaults.ChallengeType);
            entity.Property(project => project.ChallengeStep).HasDefaultValue(ChallengeDefaults.ChallengeStep);
            entity.Property(project => project.ChallengeLevel).HasDefaultValue(ChallengeDefaults.ChallengeLevel);
            entity.Property(project => project.ChallengeDescription).HasDefaultValue(ChallengeDefaults.ChallengeDescription);
            entity.Property(project => project.RequiredKnowledgeDiversity).HasDefaultValue(ChallengeDefaults.RequiredKnowledgeDiversity);
            entity.Property(project => project.RequiredCrossDomainExposure).HasDefaultValue(ChallengeDefaults.RequiredCrossDomainExposure);
            entity.Property(project => project.RequiredRewiringScore).HasDefaultValue(ChallengeDefaults.RequiredRewiringScore);
            entity.Property(project => project.ExplorationTendency).HasDefaultValue(SerendipityDefaults.ExplorationTendency);
            entity.Property(project => project.SerendipitySensitivity).HasDefaultValue(SerendipityDefaults.SerendipitySensitivity);
            entity.Property(project => project.KnowledgeRecombinationRate).HasDefaultValue(SerendipityDefaults.KnowledgeRecombinationRate);
            entity.Property(project => project.SerendipityThreshold).HasDefaultValue(SerendipityDefaults.SerendipityThreshold);
            entity.Property(project => project.EnableSerendipity).HasDefaultValue(SerendipityDefaults.EnableSerendipity);
            entity.Property(project => project.EnableTrustDynamics).HasDefaultValue(TrustDynamicsDefaults.EnableTrustDynamics);
            entity.Property(project => project.TrustGrowthRate).HasDefaultValue(TrustDynamicsDefaults.TrustGrowthRate);
            entity.Property(project => project.TrustDecayRate).HasDefaultValue(TrustDynamicsDefaults.TrustDecayRate);
            entity.Property(project => project.TrustSaturationStrength).HasDefaultValue(TrustDynamicsDefaults.TrustSaturationStrength);
            entity.Property(project => project.TrustCapacity).HasDefaultValue(TrustDynamicsDefaults.TrustCapacity);
            entity.Property(project => project.TrustCapacityPenalty).HasDefaultValue(TrustDynamicsDefaults.TrustCapacityPenalty);
            entity.Property(project => project.DistrustPenalty).HasDefaultValue(TrustDynamicsDefaults.DistrustPenalty);
            entity.Property(project => project.ConstructiveCriticismBonus).HasDefaultValue(TrustDynamicsDefaults.ConstructiveCriticismBonus);
            entity.HasOne(project => project.Experiment)
                .WithMany(experiment => experiment.SimulationProjects)
                .HasForeignKey(project => project.ExperimentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(project => project.Agents)
                .WithOne(agent => agent.SimulationProject)
                .HasForeignKey(agent => agent.SimulationProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.Steps)
                .WithOne(step => step.SimulationProject)
                .HasForeignKey(step => step.SimulationProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.TrustSnapshots)
                .WithOne(snapshot => snapshot.SimulationProject)
                .HasForeignKey(snapshot => snapshot.SimulationProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(project => project.Metrics)
                .WithOne(metrics => metrics.SimulationProject)
                .HasForeignKey<SimulationMetrics>(metrics => metrics.SimulationProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Experiment>(entity =>
        {
            entity.Property(project => project.Name).HasMaxLength(200);
            entity.Property(project => project.LlmProvider).HasMaxLength(40);
            entity.Property(project => project.LlmModel).HasMaxLength(120);
            entity.Property(project => project.Status).HasMaxLength(40);
            entity.Property(project => project.EffectiveTrustThreshold).HasDefaultValue(BoundaryParameterDefaults.EffectiveTrustThreshold);
            entity.Property(project => project.KnowledgeStock).HasDefaultValue(KnowledgeDefaults.Stock);
            entity.Property(project => project.KnowledgeDiversity).HasDefaultValue(KnowledgeDefaults.Diversity);
            entity.Property(project => project.ExternalShockLevel).HasDefaultValue(KnowledgeDefaults.ExternalShockLevel);
            entity.Property(project => project.CrossDomainExposure).HasDefaultValue(KnowledgeDefaults.CrossDomainExposure);
            entity.Property(project => project.RewiringSensitivity).HasDefaultValue(KnowledgeDefaults.RewiringSensitivity);
            entity.Property(project => project.EnableExternalShock).HasDefaultValue(KnowledgeDefaults.EnableExternalShock);
            entity.Property(project => project.ShockStep).HasDefaultValue(KnowledgeDefaults.ShockStep);
            entity.Property(project => project.ShockType).HasMaxLength(40).HasDefaultValue(KnowledgeDefaults.ShockType);
            entity.Property(project => project.ShockDescription).HasDefaultValue(KnowledgeDefaults.ShockDescription);
            entity.Property(project => project.EnableChallengeEvent).HasDefaultValue(ChallengeDefaults.EnableChallengeEvent);
            entity.Property(project => project.ChallengeType).HasMaxLength(40).HasDefaultValue(ChallengeDefaults.ChallengeType);
            entity.Property(project => project.ChallengeStep).HasDefaultValue(ChallengeDefaults.ChallengeStep);
            entity.Property(project => project.ChallengeLevel).HasDefaultValue(ChallengeDefaults.ChallengeLevel);
            entity.Property(project => project.ChallengeDescription).HasDefaultValue(ChallengeDefaults.ChallengeDescription);
            entity.Property(project => project.RequiredKnowledgeDiversity).HasDefaultValue(ChallengeDefaults.RequiredKnowledgeDiversity);
            entity.Property(project => project.RequiredCrossDomainExposure).HasDefaultValue(ChallengeDefaults.RequiredCrossDomainExposure);
            entity.Property(project => project.RequiredRewiringScore).HasDefaultValue(ChallengeDefaults.RequiredRewiringScore);
            entity.Property(project => project.ExplorationTendency).HasDefaultValue(SerendipityDefaults.ExplorationTendency);
            entity.Property(project => project.SerendipitySensitivity).HasDefaultValue(SerendipityDefaults.SerendipitySensitivity);
            entity.Property(project => project.KnowledgeRecombinationRate).HasDefaultValue(SerendipityDefaults.KnowledgeRecombinationRate);
            entity.Property(project => project.SerendipityThreshold).HasDefaultValue(SerendipityDefaults.SerendipityThreshold);
            entity.Property(project => project.EnableSerendipity).HasDefaultValue(SerendipityDefaults.EnableSerendipity);
            entity.Property(project => project.EnableTrustDynamics).HasDefaultValue(TrustDynamicsDefaults.EnableTrustDynamics);
            entity.Property(project => project.TrustGrowthRate).HasDefaultValue(TrustDynamicsDefaults.TrustGrowthRate);
            entity.Property(project => project.TrustDecayRate).HasDefaultValue(TrustDynamicsDefaults.TrustDecayRate);
            entity.Property(project => project.TrustSaturationStrength).HasDefaultValue(TrustDynamicsDefaults.TrustSaturationStrength);
            entity.Property(project => project.TrustCapacity).HasDefaultValue(TrustDynamicsDefaults.TrustCapacity);
            entity.Property(project => project.TrustCapacityPenalty).HasDefaultValue(TrustDynamicsDefaults.TrustCapacityPenalty);
            entity.Property(project => project.DistrustPenalty).HasDefaultValue(TrustDynamicsDefaults.DistrustPenalty);
            entity.Property(project => project.ConstructiveCriticismBonus).HasDefaultValue(TrustDynamicsDefaults.ConstructiveCriticismBonus);
            entity.HasOne(project => project.Scenario)
                .WithMany(scenario => scenario.Experiments)
                .HasForeignKey(project => project.ScenarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(200);
            entity.Property(item => item.LlmProvider).HasMaxLength(40);
            entity.Property(item => item.LlmModel).HasMaxLength(120);
            entity.Property(item => item.EffectiveTrustThreshold).HasDefaultValue(BoundaryParameterDefaults.EffectiveTrustThreshold);
            entity.Property(item => item.KnowledgeStock).HasDefaultValue(KnowledgeDefaults.Stock);
            entity.Property(item => item.KnowledgeDiversity).HasDefaultValue(KnowledgeDefaults.Diversity);
            entity.Property(item => item.ExternalShockLevel).HasDefaultValue(KnowledgeDefaults.ExternalShockLevel);
            entity.Property(item => item.CrossDomainExposure).HasDefaultValue(KnowledgeDefaults.CrossDomainExposure);
            entity.Property(item => item.RewiringSensitivity).HasDefaultValue(KnowledgeDefaults.RewiringSensitivity);
            entity.Property(item => item.EnableExternalShock).HasDefaultValue(KnowledgeDefaults.EnableExternalShock);
            entity.Property(item => item.ShockStep).HasDefaultValue(KnowledgeDefaults.ShockStep);
            entity.Property(item => item.ShockType).HasMaxLength(40).HasDefaultValue(KnowledgeDefaults.ShockType);
            entity.Property(item => item.ShockDescription).HasDefaultValue(KnowledgeDefaults.ShockDescription);
            entity.Property(item => item.EnableChallengeEvent).HasDefaultValue(ChallengeDefaults.EnableChallengeEvent);
            entity.Property(item => item.ChallengeType).HasMaxLength(40).HasDefaultValue(ChallengeDefaults.ChallengeType);
            entity.Property(item => item.ChallengeStep).HasDefaultValue(ChallengeDefaults.ChallengeStep);
            entity.Property(item => item.ChallengeLevel).HasDefaultValue(ChallengeDefaults.ChallengeLevel);
            entity.Property(item => item.ChallengeDescription).HasDefaultValue(ChallengeDefaults.ChallengeDescription);
            entity.Property(item => item.RequiredKnowledgeDiversity).HasDefaultValue(ChallengeDefaults.RequiredKnowledgeDiversity);
            entity.Property(item => item.RequiredCrossDomainExposure).HasDefaultValue(ChallengeDefaults.RequiredCrossDomainExposure);
            entity.Property(item => item.RequiredRewiringScore).HasDefaultValue(ChallengeDefaults.RequiredRewiringScore);
            entity.Property(item => item.ExplorationTendency).HasDefaultValue(SerendipityDefaults.ExplorationTendency);
            entity.Property(item => item.SerendipitySensitivity).HasDefaultValue(SerendipityDefaults.SerendipitySensitivity);
            entity.Property(item => item.KnowledgeRecombinationRate).HasDefaultValue(SerendipityDefaults.KnowledgeRecombinationRate);
            entity.Property(item => item.SerendipityThreshold).HasDefaultValue(SerendipityDefaults.SerendipityThreshold);
            entity.Property(item => item.EnableSerendipity).HasDefaultValue(SerendipityDefaults.EnableSerendipity);
            entity.Property(item => item.EnableTrustDynamics).HasDefaultValue(TrustDynamicsDefaults.EnableTrustDynamics);
            entity.Property(item => item.TrustGrowthRate).HasDefaultValue(TrustDynamicsDefaults.TrustGrowthRate);
            entity.Property(item => item.TrustDecayRate).HasDefaultValue(TrustDynamicsDefaults.TrustDecayRate);
            entity.Property(item => item.TrustSaturationStrength).HasDefaultValue(TrustDynamicsDefaults.TrustSaturationStrength);
            entity.Property(item => item.TrustCapacity).HasDefaultValue(TrustDynamicsDefaults.TrustCapacity);
            entity.Property(item => item.TrustCapacityPenalty).HasDefaultValue(TrustDynamicsDefaults.TrustCapacityPenalty);
            entity.Property(item => item.DistrustPenalty).HasDefaultValue(TrustDynamicsDefaults.DistrustPenalty);
            entity.Property(item => item.ConstructiveCriticismBonus).HasDefaultValue(TrustDynamicsDefaults.ConstructiveCriticismBonus);
        });

        modelBuilder.Entity<ParameterSweep>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(200);
            entity.Property(item => item.TargetParameter).HasMaxLength(120);
            entity.Property(item => item.LlmProvider).HasMaxLength(40);
            entity.Property(item => item.LlmModel).HasMaxLength(120);
            entity.Property(item => item.Status).HasMaxLength(40);
            entity.HasMany(item => item.Runs)
                .WithOne(run => run.ParameterSweep)
                .HasForeignKey(run => run.ParameterSweepId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParameterSweepRun>(entity =>
        {
            entity.HasIndex(item => new { item.ParameterSweepId, item.ParameterValue });
        });

        modelBuilder.Entity<PhaseDiagram>(entity =>
        {
            entity.Property(item => item.Name).HasMaxLength(200);
            entity.Property(item => item.XParameterName).HasMaxLength(120);
            entity.Property(item => item.XParameterDisplayName).HasMaxLength(160);
            entity.Property(item => item.YParameterName).HasMaxLength(120);
            entity.Property(item => item.YParameterDisplayName).HasMaxLength(160);
            entity.Property(item => item.AdaptiveSourceType).HasMaxLength(40).HasDefaultValue(string.Empty);
            entity.Property(item => item.AdaptiveReason).HasMaxLength(1000).HasDefaultValue(string.Empty);
            entity.Property(item => item.IsAdaptiveSweep).HasDefaultValue(false);
            entity.Property(item => item.LlmProvider).HasMaxLength(40).HasDefaultValue(LlmDefaults.Provider);
            entity.Property(item => item.LlmModel).HasMaxLength(120).HasDefaultValue(LlmDefaults.MockModel);
            entity.Property(item => item.Status).HasMaxLength(40).HasDefaultValue(PhaseDiagramStatus.Created);
            entity.HasOne(item => item.BaseScenario)
                .WithMany()
                .HasForeignKey(item => item.BaseScenarioId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.ParentPhaseDiagram)
                .WithMany()
                .HasForeignKey(item => item.ParentPhaseDiagramId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(item => item.Points)
                .WithOne(point => point.PhaseDiagram)
                .HasForeignKey(point => point.PhaseDiagramId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PhaseDiagramPoint>(entity =>
        {
            entity.Property(item => item.DominantPhase).HasMaxLength(40);
            entity.Property(item => item.DominantBottleneck).HasMaxLength(80);
            entity.HasOne(item => item.Experiment)
                .WithMany()
                .HasForeignKey(item => item.ExperimentId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(item => new { item.PhaseDiagramId, item.XValue, item.YValue }).IsUnique();
        });

        modelBuilder.Entity<ExperimentRun>(entity =>
        {
            entity.Property(run => run.FinalPhase).HasMaxLength(40);
            entity.Property(run => run.HubAgentName).HasMaxLength(120);
            entity.Property(run => run.EffectiveNetworkDensity).HasDefaultValue(0.0);
            entity.Property(run => run.StrongLinkCount).HasDefaultValue(0);
            entity.Property(run => run.WeakLinkCount).HasDefaultValue(0);
            entity.Property(run => run.ComponentCount).HasDefaultValue(0);
            entity.HasOne(run => run.Experiment)
                .WithMany(experiment => experiment.Runs)
                .HasForeignKey(run => run.ExperimentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(run => run.SimulationProject)
                .WithMany()
                .HasForeignKey(run => run.SimulationProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(run => new { run.ExperimentId, run.RunNo }).IsUnique();
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(agent => agent.Name).HasMaxLength(120);
            entity.Property(agent => agent.Role).HasMaxLength(160);
            entity.Property(agent => agent.Personality).HasMaxLength(80);
            entity.Property(agent => agent.Orientation).HasMaxLength(80);
            entity.HasMany(agent => agent.Actions)
                .WithOne(action => action.Agent)
                .HasForeignKey(action => action.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SimulationMetrics>(entity =>
        {
            entity.Property(metrics => metrics.FinalPhase).HasMaxLength(40);
            entity.Property(metrics => metrics.HubAgentName).HasMaxLength(120);
            entity.Property(metrics => metrics.EffectiveNetworkDensity).HasDefaultValue(0.0);
            entity.Property(metrics => metrics.StrongLinkCount).HasDefaultValue(0);
            entity.Property(metrics => metrics.WeakLinkCount).HasDefaultValue(0);
            entity.Property(metrics => metrics.ComponentCount).HasDefaultValue(0);
            entity.HasIndex(metrics => metrics.SimulationProjectId).IsUnique();
        });

        modelBuilder.Entity<SimulationStep>(entity =>
        {
            entity.Property(step => step.Phase).HasMaxLength(40);
            entity.HasIndex(step => new { step.SimulationProjectId, step.StepNo }).IsUnique();
        });

        modelBuilder.Entity<AgentAction>(entity =>
        {
            entity.Property(action => action.Action).HasMaxLength(40);
            entity.Property(action => action.TargetAgentName).HasMaxLength(120);
            entity.HasIndex(action => new { action.SimulationProjectId, action.StepNo });
        });

        modelBuilder.Entity<TrustSnapshot>(entity =>
        {
            entity.Property(snapshot => snapshot.SourceAgentName).HasMaxLength(120);
            entity.Property(snapshot => snapshot.TargetAgentName).HasMaxLength(120);
            entity.HasIndex(snapshot => new { snapshot.SimulationProjectId, snapshot.StepNo });
            entity.HasIndex(snapshot => new
            {
                snapshot.SimulationProjectId,
                snapshot.StepNo,
                snapshot.SourceAgentId,
                snapshot.TargetAgentId
            }).IsUnique();
        });
    }
}
