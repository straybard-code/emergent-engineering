using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ParameterSweep> ParameterSweeps => Set<ParameterSweep>();
    public DbSet<ParameterSweepRun> ParameterSweepRuns => Set<ParameterSweepRun>();
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

        modelBuilder.Entity<ExperimentRun>(entity =>
        {
            entity.Property(run => run.FinalPhase).HasMaxLength(40);
            entity.Property(run => run.HubAgentName).HasMaxLength(120);
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
