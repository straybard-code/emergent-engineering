using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703084000_AddTrustDynamicsParameters")]
    public partial class AddTrustDynamicsParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddTrustDynamicsColumns(migrationBuilder, "Experiments");
            AddTrustDynamicsColumns(migrationBuilder, "Scenarios");
            AddTrustDynamicsColumns(migrationBuilder, "SimulationProjects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveTrustDynamicsColumns(migrationBuilder, "Experiments");
            RemoveTrustDynamicsColumns(migrationBuilder, "Scenarios");
            RemoveTrustDynamicsColumns(migrationBuilder, "SimulationProjects");
        }

        private static void AddTrustDynamicsColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableTrustDynamics",
                table: tableName,
                type: "bit",
                nullable: false,
                defaultValue: TrustDynamicsDefaults.EnableTrustDynamics);

            migrationBuilder.AddColumn<double>(
                name: "TrustGrowthRate",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: TrustDynamicsDefaults.TrustGrowthRate);

            migrationBuilder.AddColumn<double>(
                name: "TrustDecayRate",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: TrustDynamicsDefaults.TrustDecayRate);

            migrationBuilder.AddColumn<double>(
                name: "TrustSaturationStrength",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: TrustDynamicsDefaults.TrustSaturationStrength);

            migrationBuilder.AddColumn<int>(
                name: "TrustCapacity",
                table: tableName,
                type: "int",
                nullable: false,
                defaultValue: TrustDynamicsDefaults.TrustCapacity);

            migrationBuilder.AddColumn<double>(
                name: "TrustCapacityPenalty",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: TrustDynamicsDefaults.TrustCapacityPenalty);

            migrationBuilder.AddColumn<double>(
                name: "DistrustPenalty",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: TrustDynamicsDefaults.DistrustPenalty);

            migrationBuilder.AddColumn<double>(
                name: "ConstructiveCriticismBonus",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: TrustDynamicsDefaults.ConstructiveCriticismBonus);
        }

        private static void RemoveTrustDynamicsColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.DropColumn(name: "EnableTrustDynamics", table: tableName);
            migrationBuilder.DropColumn(name: "TrustGrowthRate", table: tableName);
            migrationBuilder.DropColumn(name: "TrustDecayRate", table: tableName);
            migrationBuilder.DropColumn(name: "TrustSaturationStrength", table: tableName);
            migrationBuilder.DropColumn(name: "TrustCapacity", table: tableName);
            migrationBuilder.DropColumn(name: "TrustCapacityPenalty", table: tableName);
            migrationBuilder.DropColumn(name: "DistrustPenalty", table: tableName);
            migrationBuilder.DropColumn(name: "ConstructiveCriticismBonus", table: tableName);
        }
    }
}
