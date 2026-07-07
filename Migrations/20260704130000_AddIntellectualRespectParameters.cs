using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260704130000_AddIntellectualRespectParameters")]
    public partial class AddIntellectualRespectParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddIntellectualRespectColumns(migrationBuilder, "Experiments");
            AddIntellectualRespectColumns(migrationBuilder, "Scenarios");
            AddIntellectualRespectColumns(migrationBuilder, "SimulationProjects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropIntellectualRespectColumns(migrationBuilder, "SimulationProjects");
            DropIntellectualRespectColumns(migrationBuilder, "Scenarios");
            DropIntellectualRespectColumns(migrationBuilder, "Experiments");
        }

        private static void AddIntellectualRespectColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableIntellectualRespect",
                table: table,
                type: "bit",
                nullable: false,
                defaultValue: IntellectualRespectDefaults.EnableIntellectualRespect);

            migrationBuilder.AddColumn<double>(
                name: "IntellectualRespectBase",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: IntellectualRespectDefaults.Base);

            migrationBuilder.AddColumn<double>(
                name: "IntellectualRespectGrowthRate",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: IntellectualRespectDefaults.GrowthRate);

            migrationBuilder.AddColumn<double>(
                name: "IntellectualRespectDecayRate",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: IntellectualRespectDefaults.DecayRate);

            migrationBuilder.AddColumn<double>(
                name: "IntellectualRespectDiversitySensitivity",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: IntellectualRespectDefaults.DiversitySensitivity);

            migrationBuilder.AddColumn<double>(
                name: "IntellectualRespectChallengeSensitivity",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: IntellectualRespectDefaults.ChallengeSensitivity);

            migrationBuilder.AddColumn<double>(
                name: "IntellectualRespectMentorshipSensitivity",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: IntellectualRespectDefaults.MentorshipSensitivity);

            migrationBuilder.AddColumn<double>(
                name: "IntellectualRespectEgoPenalty",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: IntellectualRespectDefaults.EgoPenalty);

            migrationBuilder.AddColumn<double>(
                name: "IntellectualRespectHierarchyPenalty",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: IntellectualRespectDefaults.HierarchyPenalty);
        }

        private static void DropIntellectualRespectColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.DropColumn(name: "EnableIntellectualRespect", table: table);
            migrationBuilder.DropColumn(name: "IntellectualRespectBase", table: table);
            migrationBuilder.DropColumn(name: "IntellectualRespectGrowthRate", table: table);
            migrationBuilder.DropColumn(name: "IntellectualRespectDecayRate", table: table);
            migrationBuilder.DropColumn(name: "IntellectualRespectDiversitySensitivity", table: table);
            migrationBuilder.DropColumn(name: "IntellectualRespectChallengeSensitivity", table: table);
            migrationBuilder.DropColumn(name: "IntellectualRespectMentorshipSensitivity", table: table);
            migrationBuilder.DropColumn(name: "IntellectualRespectEgoPenalty", table: table);
            migrationBuilder.DropColumn(name: "IntellectualRespectHierarchyPenalty", table: table);
        }
    }
}
