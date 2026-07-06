using System;
using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260704090000_AddPhaseDiagrams")]
    public partial class AddPhaseDiagrams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhaseDiagrams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseScenarioId = table.Column<int>(type: "int", nullable: false),
                    XParameterName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    XParameterDisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    XStartValue = table.Column<double>(type: "float", nullable: false),
                    XEndValue = table.Column<double>(type: "float", nullable: false),
                    XStepValue = table.Column<double>(type: "float", nullable: false),
                    YParameterName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    YParameterDisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    YStartValue = table.Column<double>(type: "float", nullable: false),
                    YEndValue = table.Column<double>(type: "float", nullable: false),
                    YStepValue = table.Column<double>(type: "float", nullable: false),
                    RunsPerPoint = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PhaseDiagrams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhaseDiagrams_Scenarios_BaseScenarioId",
                        column: x => x.BaseScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhaseDiagramPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhaseDiagramId = table.Column<int>(type: "int", nullable: false),
                    XValue = table.Column<double>(type: "float", nullable: false),
                    YValue = table.Column<double>(type: "float", nullable: false),
                    ExperimentId = table.Column<int>(type: "int", nullable: true),
                    FinalPhaseSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DominantPhase = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EmergentRate = table.Column<double>(type: "float", nullable: false),
                    StableRate = table.Column<double>(type: "float", nullable: false),
                    LearningRate = table.Column<double>(type: "float", nullable: false),
                    SiloRate = table.Column<double>(type: "float", nullable: false),
                    ChaosRate = table.Column<double>(type: "float", nullable: false),
                    CollapseRate = table.Column<double>(type: "float", nullable: false),
                    AverageTrust = table.Column<double>(type: "float", nullable: false),
                    AverageEffectiveDensity = table.Column<double>(type: "float", nullable: false),
                    AverageKnowledgeDiversity = table.Column<double>(type: "float", nullable: false),
                    AverageKnowledgeRecombinationScore = table.Column<double>(type: "float", nullable: false),
                    AverageKnowledgeReconfigurationScore = table.Column<double>(type: "float", nullable: false),
                    AverageSerendipityRate = table.Column<double>(type: "float", nullable: false),
                    AveragePipelineCompletionScore = table.Column<double>(type: "float", nullable: false),
                    DominantBottleneck = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhaseDiagramPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhaseDiagramPoints_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PhaseDiagramPoints_PhaseDiagrams_PhaseDiagramId",
                        column: x => x.PhaseDiagramId,
                        principalTable: "PhaseDiagrams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhaseDiagramPoints_ExperimentId",
                table: "PhaseDiagramPoints",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseDiagramPoints_PhaseDiagramId_XValue_YValue",
                table: "PhaseDiagramPoints",
                columns: new[] { "PhaseDiagramId", "XValue", "YValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhaseDiagrams_BaseScenarioId",
                table: "PhaseDiagrams",
                column: "BaseScenarioId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhaseDiagramPoints");

            migrationBuilder.DropTable(
                name: "PhaseDiagrams");
        }
    }
}
