using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703082000_AddChallengeEventParameters")]
    public partial class AddChallengeEventParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddChallengeColumns(migrationBuilder, "Experiments");
            AddChallengeColumns(migrationBuilder, "Scenarios");
            AddChallengeColumns(migrationBuilder, "SimulationProjects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveChallengeColumns(migrationBuilder, "Experiments");
            RemoveChallengeColumns(migrationBuilder, "Scenarios");
            RemoveChallengeColumns(migrationBuilder, "SimulationProjects");
        }

        private static void AddChallengeColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChallengeDescription",
                table: tableName,
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: ChallengeDefaults.ChallengeDescription);

            migrationBuilder.AddColumn<double>(
                name: "ChallengeLevel",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: ChallengeDefaults.ChallengeLevel);

            migrationBuilder.AddColumn<int>(
                name: "ChallengeStep",
                table: tableName,
                type: "int",
                nullable: false,
                defaultValue: ChallengeDefaults.ChallengeStep);

            migrationBuilder.AddColumn<string>(
                name: "ChallengeType",
                table: tableName,
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: ChallengeDefaults.ChallengeType);

            migrationBuilder.AddColumn<bool>(
                name: "EnableChallengeEvent",
                table: tableName,
                type: "bit",
                nullable: false,
                defaultValue: ChallengeDefaults.EnableChallengeEvent);

            migrationBuilder.AddColumn<double>(
                name: "RequiredCrossDomainExposure",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: ChallengeDefaults.RequiredCrossDomainExposure);

            migrationBuilder.AddColumn<double>(
                name: "RequiredKnowledgeDiversity",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: ChallengeDefaults.RequiredKnowledgeDiversity);

            migrationBuilder.AddColumn<double>(
                name: "RequiredRewiringScore",
                table: tableName,
                type: "float",
                nullable: false,
                defaultValue: ChallengeDefaults.RequiredRewiringScore);
        }

        private static void RemoveChallengeColumns(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.DropColumn(name: "ChallengeDescription", table: tableName);
            migrationBuilder.DropColumn(name: "ChallengeLevel", table: tableName);
            migrationBuilder.DropColumn(name: "ChallengeStep", table: tableName);
            migrationBuilder.DropColumn(name: "ChallengeType", table: tableName);
            migrationBuilder.DropColumn(name: "EnableChallengeEvent", table: tableName);
            migrationBuilder.DropColumn(name: "RequiredCrossDomainExposure", table: tableName);
            migrationBuilder.DropColumn(name: "RequiredKnowledgeDiversity", table: tableName);
            migrationBuilder.DropColumn(name: "RequiredRewiringScore", table: tableName);
        }
    }
}
