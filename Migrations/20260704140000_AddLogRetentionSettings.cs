using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    public partial class AddLogRetentionSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogDetailLevel",
                table: "Scenarios",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Summary");

            migrationBuilder.AddColumn<int>(
                name: "StepLogInterval",
                table: "Scenarios",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "ActionLogInterval",
                table: "Scenarios",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "LogDetailLevel",
                table: "Experiments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Summary");

            migrationBuilder.AddColumn<int>(
                name: "StepLogInterval",
                table: "Experiments",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "ActionLogInterval",
                table: "Experiments",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "LogDetailLevel",
                table: "SimulationProjects",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.AddColumn<int>(
                name: "StepLogInterval",
                table: "SimulationProjects",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "ActionLogInterval",
                table: "SimulationProjects",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogDetailLevel",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "StepLogInterval",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "ActionLogInterval",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "LogDetailLevel",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "StepLogInterval",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "ActionLogInterval",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "LogDetailLevel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "StepLogInterval",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "ActionLogInterval",
                table: "SimulationProjects");
        }
    }
}
