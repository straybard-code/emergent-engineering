using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260704120000_AddMutualRespectParameters")]
    public partial class AddMutualRespectParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddMutualRespectColumns(migrationBuilder, "Experiments");
            AddMutualRespectColumns(migrationBuilder, "Scenarios");
            AddMutualRespectColumns(migrationBuilder, "SimulationProjects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropMutualRespectColumns(migrationBuilder, "SimulationProjects");
            DropMutualRespectColumns(migrationBuilder, "Scenarios");
            DropMutualRespectColumns(migrationBuilder, "Experiments");
        }

        private static void AddMutualRespectColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.AddColumn<double>(
                name: "MutualRespectBase",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: MutualRespectDefaults.Base);

            migrationBuilder.AddColumn<double>(
                name: "MutualRespectGrowthRate",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: MutualRespectDefaults.GrowthRate);

            migrationBuilder.AddColumn<double>(
                name: "MutualRespectDecayRate",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: MutualRespectDefaults.DecayRate);

            migrationBuilder.AddColumn<double>(
                name: "MutualRespectDiversitySensitivity",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: MutualRespectDefaults.DiversitySensitivity);

            migrationBuilder.AddColumn<double>(
                name: "MutualRespectChallengeSensitivity",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: MutualRespectDefaults.ChallengeSensitivity);

            migrationBuilder.AddColumn<double>(
                name: "MutualRespectBridgeSensitivity",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: MutualRespectDefaults.BridgeSensitivity);

            migrationBuilder.AddColumn<double>(
                name: "MutualRespectPopularityPenalty",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: MutualRespectDefaults.PopularityPenalty);
        }

        private static void DropMutualRespectColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.DropColumn(name: "MutualRespectBase", table: table);
            migrationBuilder.DropColumn(name: "MutualRespectGrowthRate", table: table);
            migrationBuilder.DropColumn(name: "MutualRespectDecayRate", table: table);
            migrationBuilder.DropColumn(name: "MutualRespectDiversitySensitivity", table: table);
            migrationBuilder.DropColumn(name: "MutualRespectChallengeSensitivity", table: table);
            migrationBuilder.DropColumn(name: "MutualRespectBridgeSensitivity", table: table);
            migrationBuilder.DropColumn(name: "MutualRespectPopularityPenalty", table: table);
        }
    }
}
