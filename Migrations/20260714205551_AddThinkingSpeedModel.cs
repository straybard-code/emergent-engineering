using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    /// <inheritdoc />
    public partial class AddThinkingSpeedModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableThinkingSpeedModel",
                table: "SimulationProjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "ThinkingSpeedBase",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "ThinkingSpeedDispersion",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OrganizationalDecisionSpeed",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "OrganizationalValidationSpeed",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<bool>(
                name: "EnableThinkingSpeedModel",
                table: "Scenarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "ThinkingSpeedBase",
                table: "Scenarios",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "ThinkingSpeedDispersion",
                table: "Scenarios",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OrganizationalDecisionSpeed",
                table: "Scenarios",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "OrganizationalValidationSpeed",
                table: "Scenarios",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<bool>(
                name: "EnableThinkingSpeedModel",
                table: "Experiments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "ThinkingSpeedBase",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "ThinkingSpeedDispersion",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OrganizationalDecisionSpeed",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "OrganizationalValidationSpeed",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableThinkingSpeedModel",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "ThinkingSpeedBase",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "ThinkingSpeedDispersion",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "OrganizationalDecisionSpeed",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "OrganizationalValidationSpeed",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "EnableThinkingSpeedModel",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "ThinkingSpeedBase",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "ThinkingSpeedDispersion",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "OrganizationalDecisionSpeed",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "OrganizationalValidationSpeed",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "EnableThinkingSpeedModel",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "ThinkingSpeedBase",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "ThinkingSpeedDispersion",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "OrganizationalDecisionSpeed",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "OrganizationalValidationSpeed",
                table: "Experiments");
        }
    }
}
