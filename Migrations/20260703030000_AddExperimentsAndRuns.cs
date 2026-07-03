using System;
using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703030000_AddExperimentsAndRuns")]
    public partial class AddExperimentsAndRuns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExperimentId",
                table: "SimulationProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExperimentRunId",
                table: "SimulationProjects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Experiments",
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExperimentId = table.Column<int>(type: "int", nullable: false),
                    SimulationProjectId = table.Column<int>(type: "int", nullable: false),
                    RunNo = table.Column<int>(type: "int", nullable: false),
                    FinalPhase = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AverageTrust = table.Column<double>(type: "float", nullable: false),
                    CompletedSteps = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExperimentRuns_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExperimentRuns_SimulationProjects_SimulationProjectId",
                        column: x => x.SimulationProjectId,
                        principalTable: "SimulationProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SimulationProjects_ExperimentId",
                table: "SimulationProjects",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentRuns_ExperimentId_RunNo",
                table: "ExperimentRuns",
                columns: new[] { "ExperimentId", "RunNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentRuns_SimulationProjectId",
                table: "ExperimentRuns",
                column: "SimulationProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationProjects_Experiments_ExperimentId",
                table: "SimulationProjects",
                column: "ExperimentId",
                principalTable: "Experiments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimulationProjects_Experiments_ExperimentId",
                table: "SimulationProjects");

            migrationBuilder.DropTable(
                name: "ExperimentRuns");

            migrationBuilder.DropTable(
                name: "Experiments");

            migrationBuilder.DropIndex(
                name: "IX_SimulationProjects_ExperimentId",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "ExperimentId",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "ExperimentRunId",
                table: "SimulationProjects");
        }
    }
}
