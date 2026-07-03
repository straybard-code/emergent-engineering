using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703083000_AddSerendipityParameters")]
    public partial class AddSerendipityParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddSerendipityColumns(migrationBuilder, "Experiments");
            AddSerendipityColumns(migrationBuilder, "Scenarios");
            AddSerendipityColumns(migrationBuilder, "SimulationProjects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveSerendipityColumns(migrationBuilder, "Experiments");
            RemoveSerendipityColumns(migrationBuilder, "Scenarios");
            RemoveSerendipityColumns(migrationBuilder, "SimulationProjects");
        }

        private static void AddSerendipityColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableSerendipity",
                table: tableName,
                type: "bit",
                nullable: false,
                defaultValue: SerendipityDefaults.EnableSerendipity);

            migrationBuilder.AddColumn<double>(
                name: "ExplorationTendency",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: SerendipityDefaults.ExplorationTendency);

            migrationBuilder.AddColumn<double>(
                name: "KnowledgeRecombinationRate",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: SerendipityDefaults.KnowledgeRecombinationRate);

            migrationBuilder.AddColumn<double>(
                name: "SerendipitySensitivity",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: SerendipityDefaults.SerendipitySensitivity);

            migrationBuilder.AddColumn<double>(
                name: "SerendipityThreshold",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: SerendipityDefaults.SerendipityThreshold);
        }

        private static void RemoveSerendipityColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.DropColumn(name: "EnableSerendipity", table: tableName);
            migrationBuilder.DropColumn(name: "ExplorationTendency", table: tableName);
            migrationBuilder.DropColumn(name: "KnowledgeRecombinationRate", table: tableName);
            migrationBuilder.DropColumn(name: "SerendipitySensitivity", table: tableName);
            migrationBuilder.DropColumn(name: "SerendipityThreshold", table: tableName);
        }
    }
}
