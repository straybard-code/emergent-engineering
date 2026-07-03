using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703061000_AddLlmSettings")]
    public partial class AddLlmSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LlmModel",
                table: "SimulationProjects",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "mock-v1");

            migrationBuilder.AddColumn<string>(
                name: "LlmProvider",
                table: "SimulationProjects",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Mock");

            migrationBuilder.AddColumn<string>(
                name: "LlmModel",
                table: "Experiments",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "mock-v1");

            migrationBuilder.AddColumn<string>(
                name: "LlmProvider",
                table: "Experiments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Mock");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LlmModel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "LlmProvider",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "LlmModel",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "LlmProvider",
                table: "Experiments");
        }
    }
}
