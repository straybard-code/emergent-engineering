using System;
using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

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

            modelBuilder.Entity("EmergentEngineering.Models.Experiment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AgentCount")
                        .HasColumnType("int");

                    b.Property<string>("BoundaryConditions")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("CompetitionLevel")
                        .HasColumnType("float");

                    b.Property<double>("CooperationLevel")
                        .HasColumnType("float");

                    b.Property<double>("CrossDomainExposure")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<double>("CustomerOrientationLevel")
                        .HasColumnType("float");

                    b.Property<bool>("EnableExternalShock")
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<bool>("EnableChallengeEvent")
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("EffectiveTrustThreshold")
                        .HasColumnType("float")
                        .HasDefaultValue(0.3);

                    b.Property<double>("ExternalShockLevel")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<double>("InformationSharingLevel")
                        .HasColumnType("float");

                    b.Property<string>("KpiDefinition")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("KnowledgeDiversity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.3);

                    b.Property<double>("KnowledgeStock")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<double>("LearningOrientationLevel")
                        .HasColumnType("float");

                    b.Property<string>("LlmModel")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("LlmProvider")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("Purpose")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("PsychologicalSafetyLevel")
                        .HasColumnType("float");

                    b.Property<double>("RewiringSensitivity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.5);

                    b.Property<string>("ChallengeDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("");

                    b.Property<double>("ChallengeLevel")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<int>("ChallengeStep")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("ChallengeType")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)")
                        .HasDefaultValue("None");

                    b.Property<int>("RunCount")
                        .HasColumnType("int");

                    b.Property<int?>("ScenarioId")
                        .HasColumnType("int");

                    b.Property<string>("ShockDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("");

                    b.Property<int>("ShockStep")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("ShockType")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)")
                        .HasDefaultValue("None");

                    b.Property<double>("ShortTermResultPressureLevel")
                        .HasColumnType("float");

                    b.Property<double>("RequiredCrossDomainExposure")
                        .HasColumnType("float")
                        .HasDefaultValue(0.5);

                    b.Property<double>("RequiredKnowledgeDiversity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.5);

                    b.Property<double>("RequiredRewiringScore")
                        .HasColumnType("float")
                        .HasDefaultValue(0.3);

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<int>("TotalSteps")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ScenarioId");

                    b.ToTable("Experiments");
                });

            modelBuilder.Entity("EmergentEngineering.Models.ExperimentRun", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<double>("AverageTrust")
                        .HasColumnType("float");

                    b.Property<int>("CompletedSteps")
                        .HasColumnType("int");

                    b.Property<int>("ComponentCount")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<double>("CriticizeSupportRatio")
                        .HasColumnType("float");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ExperimentId")
                        .HasColumnType("int");

                    b.Property<double>("EffectiveNetworkDensity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<string>("FinalPhase")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<string>("HubAgentName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<double>("HubScore")
                        .HasColumnType("float");

                    b.Property<int>("IsolatedAgentCount")
                        .HasColumnType("int");

                    b.Property<double>("NetworkDensity")
                        .HasColumnType("float");

                    b.Property<int>("PhaseChangeCount")
                        .HasColumnType("int");

                    b.Property<double>("PhaseStability")
                        .HasColumnType("float");

                    b.Property<double>("ProposeIdeaRate")
                        .HasColumnType("float");

                    b.Property<int>("RunNo")
                        .HasColumnType("int");

                    b.Property<double>("ShareInfoRate")
                        .HasColumnType("float");

                    b.Property<int>("SimulationProjectId")
                        .HasColumnType("int");

                    b.Property<int>("StrongLinkCount")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int?>("StepsToEmergent")
                        .HasColumnType("int");

                    b.Property<int?>("StepsToLearning")
                        .HasColumnType("int");

                    b.Property<int>("WeakLinkCount")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.HasKey("Id");

                    b.HasIndex("ExperimentId", "RunNo")
                        .IsUnique();

                    b.HasIndex("SimulationProjectId");

                    b.ToTable("ExperimentRuns");
                });

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

                    b.Property<string>("Orientation")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

                    b.Property<string>("Personality")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("nvarchar(80)");

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

                    b.Property<double?>("TrustAfter")
                        .HasColumnType("float");

                    b.Property<double?>("TrustBefore")
                        .HasColumnType("float");

                    b.Property<double?>("TrustDelta")
                        .HasColumnType("float");

                    b.HasKey("Id");

                    b.HasIndex("AgentId");

                    b.HasIndex("SimulationProjectId", "StepNo");

                    b.ToTable("AgentActions");
                });

            modelBuilder.Entity("EmergentEngineering.Models.ParameterSweep", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AgentCount")
                        .HasColumnType("int");

                    b.Property<DateTime?>("CompletedAt")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("EndValue")
                        .HasColumnType("float");

                    b.Property<string>("LlmModel")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("LlmProvider")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("RunCountPerValue")
                        .HasColumnType("int");

                    b.Property<int>("ScenarioId")
                        .HasColumnType("int");

                    b.Property<double>("StartValue")
                        .HasColumnType("float");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<double>("StepValue")
                        .HasColumnType("float");

                    b.Property<string>("TargetParameter")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<int>("TotalSteps")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("ParameterSweeps");
                });

            modelBuilder.Entity("EmergentEngineering.Models.ParameterSweepRun", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<double>("AverageAbsTrust")
                        .HasColumnType("float");

                    b.Property<double>("AverageTrust")
                        .HasColumnType("float");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ExperimentId")
                        .HasColumnType("int");

                    b.Property<string>("FinalPhaseSummaryJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("ParameterValue")
                        .HasColumnType("float");

                    b.Property<int>("ParameterSweepId")
                        .HasColumnType("int");

                    b.Property<int>("RunCount")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ParameterSweepId", "ParameterValue");

                    b.ToTable("ParameterSweepRuns");
                });

            modelBuilder.Entity("EmergentEngineering.Models.SimulationMetrics", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<double>("AverageTrust")
                        .HasColumnType("float");

                    b.Property<double>("CriticizeSupportRatio")
                        .HasColumnType("float");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("ComponentCount")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("FinalPhase")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<string>("HubAgentName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<double>("HubScore")
                        .HasColumnType("float");

                    b.Property<double>("EffectiveNetworkDensity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<int>("IsolatedAgentCount")
                        .HasColumnType("int");

                    b.Property<double>("NetworkDensity")
                        .HasColumnType("float");

                    b.Property<int>("PhaseChangeCount")
                        .HasColumnType("int");

                    b.Property<double>("PhaseStability")
                        .HasColumnType("float");

                    b.Property<double>("ProposeIdeaRate")
                        .HasColumnType("float");

                    b.Property<double>("ShareInfoRate")
                        .HasColumnType("float");

                    b.Property<int>("SimulationProjectId")
                        .HasColumnType("int");

                    b.Property<int>("StrongLinkCount")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<int?>("StepsToEmergent")
                        .HasColumnType("int");

                    b.Property<int?>("StepsToLearning")
                        .HasColumnType("int");

                    b.Property<int>("WeakLinkCount")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.HasKey("Id");

                    b.HasIndex("SimulationProjectId")
                        .IsUnique();

                    b.ToTable("SimulationMetrics");
                });

            modelBuilder.Entity("EmergentEngineering.Models.TrustSnapshot", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("SimulationProjectId")
                        .HasColumnType("int");

                    b.Property<int>("SourceAgentId")
                        .HasColumnType("int");

                    b.Property<string>("SourceAgentName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<int>("StepNo")
                        .HasColumnType("int");

                    b.Property<int>("TargetAgentId")
                        .HasColumnType("int");

                    b.Property<string>("TargetAgentName")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<double>("TrustValue")
                        .HasColumnType("float");

                    b.HasKey("Id");

                    b.HasIndex("SimulationProjectId", "StepNo");

                    b.HasIndex("SimulationProjectId", "StepNo", "SourceAgentId", "TargetAgentId")
                        .IsUnique();

                    b.ToTable("TrustSnapshots");
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

                    b.Property<double>("CompetitionLevel")
                        .HasColumnType("float");

                    b.Property<double>("CooperationLevel")
                        .HasColumnType("float");

                    b.Property<double>("CrossDomainExposure")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("CurrentStep")
                        .HasColumnType("int");

                    b.Property<double>("CustomerOrientationLevel")
                        .HasColumnType("float");

                    b.Property<bool>("EnableExternalShock")
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<bool>("EnableChallengeEvent")
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<double>("EffectiveTrustThreshold")
                        .HasColumnType("float")
                        .HasDefaultValue(0.3);

                    b.Property<double>("ExternalShockLevel")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<int?>("ExperimentId")
                        .HasColumnType("int");

                    b.Property<int?>("ExperimentRunId")
                        .HasColumnType("int");

                    b.Property<double>("InformationSharingLevel")
                        .HasColumnType("float");

                    b.Property<string>("KpiDefinition")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("KnowledgeDiversity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.3);

                    b.Property<double>("KnowledgeStock")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<double>("LearningOrientationLevel")
                        .HasColumnType("float");

                    b.Property<string>("LlmModel")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("LlmProvider")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

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

                    b.Property<double>("PsychologicalSafetyLevel")
                        .HasColumnType("float");

                    b.Property<double>("RewiringSensitivity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.5);

                    b.Property<string>("ChallengeDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("");

                    b.Property<double>("ChallengeLevel")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<int>("ChallengeStep")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("ChallengeType")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)")
                        .HasDefaultValue("None");

                    b.Property<string>("ShockDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("");

                    b.Property<int>("ShockStep")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("ShockType")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)")
                        .HasDefaultValue("None");

                    b.Property<double>("ShortTermResultPressureLevel")
                        .HasColumnType("float");

                    b.Property<double>("RequiredCrossDomainExposure")
                        .HasColumnType("float")
                        .HasDefaultValue(0.5);

                    b.Property<double>("RequiredKnowledgeDiversity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.5);

                    b.Property<double>("RequiredRewiringScore")
                        .HasColumnType("float")
                        .HasDefaultValue(0.3);

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<int>("TotalSteps")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ExperimentId");

                    b.ToTable("SimulationProjects");
                });

            modelBuilder.Entity("EmergentEngineering.Models.Scenario", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("AgentCount")
                        .HasColumnType("int");

                    b.Property<string>("BoundaryConditions")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("CompetitionLevel")
                        .HasColumnType("float");

                    b.Property<double>("CooperationLevel")
                        .HasColumnType("float");

                    b.Property<double>("CrossDomainExposure")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<double>("CustomerOrientationLevel")
                        .HasColumnType("float");

                    b.Property<bool>("EnableExternalShock")
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<bool>("EnableChallengeEvent")
                        .HasColumnType("bit")
                        .HasDefaultValue(false);

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("EffectiveTrustThreshold")
                        .HasColumnType("float")
                        .HasDefaultValue(0.3);

                    b.Property<double>("ExternalShockLevel")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<double>("InformationSharingLevel")
                        .HasColumnType("float");

                    b.Property<string>("KpiDefinition")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("KnowledgeDiversity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.3);

                    b.Property<double>("KnowledgeStock")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<double>("LearningOrientationLevel")
                        .HasColumnType("float");

                    b.Property<string>("LlmModel")
                        .IsRequired()
                        .HasMaxLength(120)
                        .HasColumnType("nvarchar(120)");

                    b.Property<string>("LlmProvider")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<double>("PsychologicalSafetyLevel")
                        .HasColumnType("float");

                    b.Property<string>("Purpose")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("RewiringSensitivity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.5);

                    b.Property<string>("ChallengeDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("");

                    b.Property<double>("ChallengeLevel")
                        .HasColumnType("float")
                        .HasDefaultValue(0.0);

                    b.Property<int>("ChallengeStep")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("ChallengeType")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)")
                        .HasDefaultValue("None");

                    b.Property<int>("RunCount")
                        .HasColumnType("int");

                    b.Property<string>("ShockDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasDefaultValue("");

                    b.Property<int>("ShockStep")
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<string>("ShockType")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)")
                        .HasDefaultValue("None");

                    b.Property<double>("ShortTermResultPressureLevel")
                        .HasColumnType("float");

                    b.Property<double>("RequiredCrossDomainExposure")
                        .HasColumnType("float")
                        .HasDefaultValue(0.5);

                    b.Property<double>("RequiredKnowledgeDiversity")
                        .HasColumnType("float")
                        .HasDefaultValue(0.5);

                    b.Property<double>("RequiredRewiringScore")
                        .HasColumnType("float")
                        .HasDefaultValue(0.3);

                    b.Property<int>("TotalSteps")
                        .HasColumnType("int");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Scenarios");
                });

            modelBuilder.Entity("EmergentEngineering.Models.SimulationStep", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Phase")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

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

            modelBuilder.Entity("EmergentEngineering.Models.ExperimentRun", b =>
                {
                    b.HasOne("EmergentEngineering.Models.Experiment", "Experiment")
                        .WithMany("Runs")
                        .HasForeignKey("ExperimentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("EmergentEngineering.Models.SimulationProject", "SimulationProject")
                        .WithMany()
                        .HasForeignKey("SimulationProjectId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Experiment");
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

            modelBuilder.Entity("EmergentEngineering.Models.ParameterSweepRun", b =>
                {
                    b.HasOne("EmergentEngineering.Models.ParameterSweep", "ParameterSweep")
                        .WithMany("Runs")
                        .HasForeignKey("ParameterSweepId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParameterSweep");
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

            modelBuilder.Entity("EmergentEngineering.Models.SimulationMetrics", b =>
                {
                    b.HasOne("EmergentEngineering.Models.SimulationProject", "SimulationProject")
                        .WithOne("Metrics")
                        .HasForeignKey("EmergentEngineering.Models.SimulationMetrics", "SimulationProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SimulationProject");
                });

            modelBuilder.Entity("EmergentEngineering.Models.TrustSnapshot", b =>
                {
                    b.HasOne("EmergentEngineering.Models.SimulationProject", "SimulationProject")
                        .WithMany("TrustSnapshots")
                        .HasForeignKey("SimulationProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SimulationProject");
                });

            modelBuilder.Entity("EmergentEngineering.Models.Experiment", b =>
                {
                    b.HasOne("EmergentEngineering.Models.Scenario", "Scenario")
                        .WithMany("Experiments")
                        .HasForeignKey("ScenarioId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Scenario");
                });

            modelBuilder.Entity("EmergentEngineering.Models.SimulationProject", b =>
                {
                    b.HasOne("EmergentEngineering.Models.Experiment", "Experiment")
                        .WithMany("SimulationProjects")
                        .HasForeignKey("ExperimentId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("Experiment");
                });

            modelBuilder.Entity("EmergentEngineering.Models.Agent", b =>
                {
                    b.Navigation("Actions");
                });

            modelBuilder.Entity("EmergentEngineering.Models.Experiment", b =>
                {
                    b.Navigation("Runs");
                    b.Navigation("SimulationProjects");
                });

            modelBuilder.Entity("EmergentEngineering.Models.ParameterSweep", b =>
                {
                    b.Navigation("Runs");
                });

            modelBuilder.Entity("EmergentEngineering.Models.Scenario", b =>
                {
                    b.Navigation("Experiments");
                });

            modelBuilder.Entity("EmergentEngineering.Models.SimulationProject", b =>
                {
                    b.Navigation("Agents");
                    b.Navigation("Metrics");
                    b.Navigation("Steps");
                    b.Navigation("TrustSnapshots");
                });
#pragma warning restore 612, 618
        }
    }
}
