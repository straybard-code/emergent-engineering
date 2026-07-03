using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703081000_AddKnowledgeAndShockParameters")]
    public partial class AddKnowledgeAndShockParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddKnowledgeAndShockColumns(migrationBuilder, "Experiments");
            AddKnowledgeAndShockColumns(migrationBuilder, "Scenarios");
            AddKnowledgeAndShockColumns(migrationBuilder, "SimulationProjects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveKnowledgeAndShockColumns(migrationBuilder, "Experiments");
            RemoveKnowledgeAndShockColumns(migrationBuilder, "Scenarios");
            RemoveKnowledgeAndShockColumns(migrationBuilder, "SimulationProjects");
        }

        private static void AddKnowledgeAndShockColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.AddColumn<double>(
                name: "CrossDomainExposure",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: KnowledgeDefaults.CrossDomainExposure);

            migrationBuilder.AddColumn<bool>(
                name: "EnableExternalShock",
                table: tableName,
                type: "bit",
                nullable: false,
                defaultValue: KnowledgeDefaults.EnableExternalShock);

            migrationBuilder.AddColumn<double>(
                name: "ExternalShockLevel",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: KnowledgeDefaults.ExternalShockLevel);

            migrationBuilder.AddColumn<double>(
                name: "KnowledgeDiversity",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: KnowledgeDefaults.Diversity);

            migrationBuilder.AddColumn<double>(
                name: "KnowledgeStock",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: KnowledgeDefaults.Stock);

            migrationBuilder.AddColumn<double>(
                name: "RewiringSensitivity",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: KnowledgeDefaults.RewiringSensitivity);

            migrationBuilder.AddColumn<string>(
                name: "ShockDescription",
                table: tableName,
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: KnowledgeDefaults.ShockDescription);

            migrationBuilder.AddColumn<int>(
                name: "ShockStep",
                table: tableName,
                type: "int",
                nullable: false,
                defaultValue: KnowledgeDefaults.ShockStep);

            migrationBuilder.AddColumn<string>(
                name: "ShockType",
                table: tableName,
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: KnowledgeDefaults.ShockType);
        }

        private static void RemoveKnowledgeAndShockColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.DropColumn(name: "CrossDomainExposure", table: tableName);
            migrationBuilder.DropColumn(name: "EnableExternalShock", table: tableName);
            migrationBuilder.DropColumn(name: "ExternalShockLevel", table: tableName);
            migrationBuilder.DropColumn(name: "KnowledgeDiversity", table: tableName);
            migrationBuilder.DropColumn(name: "KnowledgeStock", table: tableName);
            migrationBuilder.DropColumn(name: "RewiringSensitivity", table: tableName);
            migrationBuilder.DropColumn(name: "ShockDescription", table: tableName);
            migrationBuilder.DropColumn(name: "ShockStep", table: tableName);
            migrationBuilder.DropColumn(name: "ShockType", table: tableName);
        }
    }
}
