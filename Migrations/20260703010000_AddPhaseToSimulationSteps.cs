using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703010000_AddPhaseToSimulationSteps")]
    public partial class AddPhaseToSimulationSteps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "SimulationSteps",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Forming");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Phase",
                table: "SimulationSteps");
        }
    }
}
