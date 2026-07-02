using System;
using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.22");
            modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.UseIdentityColumns();

            modelBuilder.Entity("EmergentEngineering.Models.Agent", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Memory")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<double>("PositionX")
                        .HasColumnType("float");

                    b.Property<double>("PositionY")
                        .HasColumnType("float");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(160)
                        .HasColumnType("nvarchar(160)");

                    b.Property<int>("SimulationProjectId")
                        .HasColumnType("int");

                    b.Property<string>("TrustJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("SimulationProjectId");

                    b.ToTable("Agents");
                });

            modelBuilder.Entity("EmergentEngineering.Models.AgentAction", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Action")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<int>("AgentId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Memory")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RawLlmResponse")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SimulationProjectId")
                        .HasColumnType("int");

                    b.Property<int>("StepNo")
                        .HasColumnType("int");

                    b.Property<string>("TargetAgentName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.HasKey("Id");

                    b.HasIndex("AgentId");

                    b.HasIndex("SimulationProjectId", "StepNo");

                    b.ToTable("AgentActions");
                });

            modelBuilder.Entity("EmergentEngineering.Models.SimulationProject", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AgentCount")
                        .HasColumnType("int");

                    b.Property<string>("BoundaryConditions")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("CurrentStep")
                        .HasColumnType("int");

                    b.Property<string>("KpiDefinition")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Phase")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<string>("Purpose")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<int>("TotalSteps")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("SimulationProjects");
                });

            modelBuilder.Entity("EmergentEngineering.Models.SimulationStep", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("SimulationProjectId")
                        .HasColumnType("int");

                    b.Property<string>("StateJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("StepNo")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SimulationProjectId", "StepNo")
                        .IsUnique();

                    b.ToTable("SimulationSteps");
                });

            modelBuilder.Entity("EmergentEngineering.Models.Agent", b =>
                {
                    b.HasOne("EmergentEngineering.Models.SimulationProject", "SimulationProject")
                        .WithMany("Agents")
                        .HasForeignKey("SimulationProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SimulationProject");
                });

            modelBuilder.Entity("EmergentEngineering.Models.AgentAction", b =>
                {
                    b.HasOne("EmergentEngineering.Models.Agent", "Agent")
                        .WithMany("Actions")
                        .HasForeignKey("AgentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Agent");
                });

            modelBuilder.Entity("EmergentEngineering.Models.SimulationStep", b =>
                {
                    b.HasOne("EmergentEngineering.Models.SimulationProject", "SimulationProject")
                        .WithMany("Steps")
                        .HasForeignKey("SimulationProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SimulationProject");
                });

            modelBuilder.Entity("EmergentEngineering.Models.Agent", b =>
                {
                    b.Navigation("Actions");
                });

            modelBuilder.Entity("EmergentEngineering.Models.SimulationProject", b =>
                {
                    b.Navigation("Agents");
                    b.Navigation("Steps");
                });
#pragma warning restore 612, 618
        }
    }
}
