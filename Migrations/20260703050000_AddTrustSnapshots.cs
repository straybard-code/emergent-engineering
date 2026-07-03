using System;
using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703050000_AddTrustSnapshots")]
    public partial class AddTrustSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrustSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SimulationProjectId = table.Column<int>(type: "int", nullable: false),
                    StepNo = table.Column<int>(type: "int", nullable: false),
                    SourceAgentId = table.Column<int>(type: "int", nullable: false),
                    TargetAgentId = table.Column<int>(type: "int", nullable: false),
                    SourceAgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TargetAgentName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TrustValue = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrustSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrustSnapshots_SimulationProjects_SimulationProjectId",
                        column: x => x.SimulationProjectId,
                        principalTable: "SimulationProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrustSnapshots_SimulationProjectId_StepNo",
                table: "TrustSnapshots",
                columns: new[] { "SimulationProjectId", "StepNo" });

            migrationBuilder.CreateIndex(
                name: "IX_TrustSnapshots_SimulationProjectId_StepNo_SourceAgentId_TargetAgentId",
                table: "TrustSnapshots",
                columns: new[] { "SimulationProjectId", "StepNo", "SourceAgentId", "TargetAgentId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrustSnapshots");
        }
    }
}
