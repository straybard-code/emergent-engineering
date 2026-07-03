using System;
using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703062000_AddScenariosAndBoundaryParameters")]
    public partial class AddScenariosAndBoundaryParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CompetitionLevel",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "CooperationLevel",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "CustomerOrientationLevel",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "InformationSharingLevel",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "LearningOrientationLevel",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "PsychologicalSafetyLevel",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "ShortTermResultPressureLevel",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "CompetitionLevel",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "CooperationLevel",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "CustomerOrientationLevel",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "InformationSharingLevel",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "LearningOrientationLevel",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "PsychologicalSafetyLevel",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<int>(
                name: "ScenarioId",
                table: "Experiments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ShortTermResultPressureLevel",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.CreateTable(
                name: "Scenarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BoundaryConditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KpiDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgentCount = table.Column<int>(type: "int", nullable: false),
                    TotalSteps = table.Column<int>(type: "int", nullable: false),
                    RunCount = table.Column<int>(type: "int", nullable: false),
                    LlmProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Mock"),
                    LlmModel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, defaultValue: "mock-v1"),
                    InformationSharingLevel = table.Column<double>(type: "float", nullable: false, defaultValue: 0.5),
                    CooperationLevel = table.Column<double>(type: "float", nullable: false, defaultValue: 0.5),
                    CompetitionLevel = table.Column<double>(type: "float", nullable: false, defaultValue: 0.5),
                    PsychologicalSafetyLevel = table.Column<double>(type: "float", nullable: false, defaultValue: 0.5),
                    LearningOrientationLevel = table.Column<double>(type: "float", nullable: false, defaultValue: 0.5),
                    CustomerOrientationLevel = table.Column<double>(type: "float", nullable: false, defaultValue: 0.5),
                    ShortTermResultPressureLevel = table.Column<double>(type: "float", nullable: false, defaultValue: 0.5),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scenarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_ScenarioId",
                table: "Experiments",
                column: "ScenarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Experiments_Scenarios_ScenarioId",
                table: "Experiments",
                column: "ScenarioId",
                principalTable: "Scenarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Experiments_Scenarios_ScenarioId",
                table: "Experiments");

            migrationBuilder.DropTable(
                name: "Scenarios");

            migrationBuilder.DropIndex(
                name: "IX_Experiments_ScenarioId",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "CompetitionLevel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "CooperationLevel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "CustomerOrientationLevel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "InformationSharingLevel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "LearningOrientationLevel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "PsychologicalSafetyLevel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "ShortTermResultPressureLevel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "CompetitionLevel",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "CooperationLevel",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "CustomerOrientationLevel",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "InformationSharingLevel",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "LearningOrientationLevel",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "PsychologicalSafetyLevel",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "ScenarioId",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "ShortTermResultPressureLevel",
                table: "Experiments");
        }
    }
}
