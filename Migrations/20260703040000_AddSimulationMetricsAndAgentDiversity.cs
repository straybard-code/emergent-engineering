using System;
using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703040000_AddSimulationMetricsAndAgentDiversity")]
    public partial class AddSimulationMetricsAndAgentDiversity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Orientation",
                table: "Agents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Personality",
                table: "Agents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "CriticizeSupportRatio",
                table: "ExperimentRuns",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "HubAgentName",
                table: "ExperimentRuns",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "-");

            migrationBuilder.AddColumn<double>(
                name: "HubScore",
                table: "ExperimentRuns",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "IsolatedAgentCount",
                table: "ExperimentRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "NetworkDensity",
                table: "ExperimentRuns",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "PhaseChangeCount",
                table: "ExperimentRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "PhaseStability",
                table: "ExperimentRuns",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ProposeIdeaRate",
                table: "ExperimentRuns",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ShareInfoRate",
                table: "ExperimentRuns",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "StepsToEmergent",
                table: "ExperimentRuns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StepsToLearning",
                table: "ExperimentRuns",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SimulationMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SimulationProjectId = table.Column<int>(type: "int", nullable: false),
                    FinalPhase = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AverageTrust = table.Column<double>(type: "float", nullable: false),
                    NetworkDensity = table.Column<double>(type: "float", nullable: false),
                    IsolatedAgentCount = table.Column<int>(type: "int", nullable: false),
                    HubAgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HubScore = table.Column<double>(type: "float", nullable: false),
                    ShareInfoRate = table.Column<double>(type: "float", nullable: false),
                    ProposeIdeaRate = table.Column<double>(type: "float", nullable: false),
                    CriticizeSupportRatio = table.Column<double>(type: "float", nullable: false),
                    StepsToEmergent = table.Column<int>(type: "int", nullable: true),
                    StepsToLearning = table.Column<int>(type: "int", nullable: true),
                    PhaseChangeCount = table.Column<int>(type: "int", nullable: false),
                    PhaseStability = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationMetrics_SimulationProjects_SimulationProjectId",
                        column: x => x.SimulationProjectId,
                        principalTable: "SimulationProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationMetrics_SimulationProjectId",
                table: "SimulationMetrics",
                column: "SimulationProjectId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimulationMetrics");

            migrationBuilder.DropColumn(
                name: "Orientation",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Personality",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "CriticizeSupportRatio",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "HubAgentName",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "HubScore",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "IsolatedAgentCount",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "NetworkDensity",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "PhaseChangeCount",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "PhaseStability",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "ProposeIdeaRate",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "ShareInfoRate",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "StepsToEmergent",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "StepsToLearning",
                table: "ExperimentRuns");
        }
    }
}
