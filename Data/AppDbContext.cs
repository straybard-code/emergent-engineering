using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore;

namespace EmergentEngineering.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SimulationProject> SimulationProjects => Set<SimulationProject>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<SimulationStep> SimulationSteps => Set<SimulationStep>();
    public DbSet<AgentAction> AgentActions => Set<AgentAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimulationProject>(entity =>
        {
            entity.Property(project => project.Name).HasMaxLength(200);
            entity.Property(project => project.Status).HasMaxLength(40);
            entity.Property(project => project.Phase).HasMaxLength(40);
            entity.HasMany(project => project.Agents)
                .WithOne(agent => agent.SimulationProject)
                .HasForeignKey(agent => agent.SimulationProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(project => project.Steps)
                .WithOne(step => step.SimulationProject)
                .HasForeignKey(step => step.SimulationProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.Property(agent => agent.Name).HasMaxLength(120);
            entity.Property(agent => agent.Role).HasMaxLength(160);
            entity.HasMany(agent => agent.Actions)
                .WithOne(action => action.Agent)
                .HasForeignKey(action => action.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
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
    }
}
