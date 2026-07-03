using System;
using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703070000_AddParameterSweeps")]
    public partial class AddParameterSweeps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParameterSweeps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScenarioId = table.Column<int>(type: "int", nullable: false),
                    TargetParameter = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StartValue = table.Column<double>(type: "float", nullable: false),
                    EndValue = table.Column<double>(type: "float", nullable: false),
                    StepValue = table.Column<double>(type: "float", nullable: false),
                    RunCountPerValue = table.Column<int>(type: "int", nullable: false),
                    AgentCount = table.Column<int>(type: "int", nullable: false),
                    TotalSteps = table.Column<int>(type: "int", nullable: false),
                    LlmProvider = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Mock"),
                    LlmModel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false, defaultValue: "mock-v1"),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Created"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterSweeps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParameterSweepRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParameterSweepId = table.Column<int>(type: "int", nullable: false),
                    ParameterValue = table.Column<double>(type: "float", nullable: false),
                    ExperimentId = table.Column<int>(type: "int", nullable: false),
                    RunCount = table.Column<int>(type: "int", nullable: false),
                    FinalPhaseSummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AverageTrust = table.Column<double>(type: "float", nullable: false),
                    AverageAbsTrust = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterSweepRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterSweepRuns_ParameterSweeps_ParameterSweepId",
                        column: x => x.ParameterSweepId,
                        principalTable: "ParameterSweeps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParameterSweepRuns_ParameterSweepId_ParameterValue",
                table: "ParameterSweepRuns",
                columns: new[] { "ParameterSweepId", "ParameterValue" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParameterSweepRuns");

            migrationBuilder.DropTable(
                name: "ParameterSweeps");
        }
    }
}
