using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703080000_AddEffectiveNetworkMetrics")]
    public partial class AddEffectiveNetworkMetrics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EffectiveTrustThreshold",
                table: "SimulationProjects",
                type: "float",
                nullable: false,
                defaultValue: 0.3);

            migrationBuilder.AddColumn<double>(
                name: "EffectiveTrustThreshold",
                table: "Scenarios",
                type: "float",
                nullable: false,
                defaultValue: 0.3);

            migrationBuilder.AddColumn<int>(
                name: "ComponentCount",
                table: "SimulationMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "EffectiveNetworkDensity",
                table: "SimulationMetrics",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "StrongLinkCount",
                table: "SimulationMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WeakLinkCount",
                table: "SimulationMetrics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "EffectiveTrustThreshold",
                table: "Experiments",
                type: "float",
                nullable: false,
                defaultValue: 0.3);

            migrationBuilder.AddColumn<int>(
                name: "ComponentCount",
                table: "ExperimentRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "EffectiveNetworkDensity",
                table: "ExperimentRuns",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "StrongLinkCount",
                table: "ExperimentRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WeakLinkCount",
                table: "ExperimentRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectiveTrustThreshold",
                table: "SimulationProjects");

            migrationBuilder.DropColumn(
                name: "EffectiveTrustThreshold",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "ComponentCount",
                table: "SimulationMetrics");

            migrationBuilder.DropColumn(
                name: "EffectiveNetworkDensity",
                table: "SimulationMetrics");

            migrationBuilder.DropColumn(
                name: "StrongLinkCount",
                table: "SimulationMetrics");

            migrationBuilder.DropColumn(
                name: "WeakLinkCount",
                table: "SimulationMetrics");

            migrationBuilder.DropColumn(
                name: "EffectiveTrustThreshold",
                table: "Experiments");

            migrationBuilder.DropColumn(
                name: "ComponentCount",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "EffectiveNetworkDensity",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "StrongLinkCount",
                table: "ExperimentRuns");

            migrationBuilder.DropColumn(
                name: "WeakLinkCount",
                table: "ExperimentRuns");
        }
    }
}
