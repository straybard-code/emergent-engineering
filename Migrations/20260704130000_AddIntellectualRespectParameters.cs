using System;
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
            AddMutualRespectColumns(migrationBuilder, "Scenarios");
            AddMutualRespectColumns(migrationBuilder, "Experiments");
            AddMutualRespectColumns(migrationBuilder, "SimulationProjects");

            AddIntellectualRespectColumns(migrationBuilder, "Scenarios");
            AddIntellectualRespectColumns(migrationBuilder, "Experiments");
            AddIntellectualRespectColumns(migrationBuilder, "SimulationProjects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropIntellectualRespectColumns(migrationBuilder, "SimulationProjects");
            DropIntellectualRespectColumns(migrationBuilder, "Experiments");
            DropIntellectualRespectColumns(migrationBuilder, "Scenarios");

            DropMutualRespectColumns(migrationBuilder, "SimulationProjects");
            DropMutualRespectColumns(migrationBuilder, "Experiments");
            DropMutualRespectColumns(migrationBuilder, "Scenarios");
        }

        private static void AddMutualRespectColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.Sql($@"
IF COL_LENGTH('{table}', 'MutualRespectBase') IS NULL
    ALTER TABLE [{table}] ADD [MutualRespectBase] float NOT NULL CONSTRAINT [DF_{table}_MutualRespectBase] DEFAULT (0.30);
IF COL_LENGTH('{table}', 'MutualRespectGrowthRate') IS NULL
    ALTER TABLE [{table}] ADD [MutualRespectGrowthRate] float NOT NULL CONSTRAINT [DF_{table}_MutualRespectGrowthRate] DEFAULT (0.04);
IF COL_LENGTH('{table}', 'MutualRespectDecayRate') IS NULL
    ALTER TABLE [{table}] ADD [MutualRespectDecayRate] float NOT NULL CONSTRAINT [DF_{table}_MutualRespectDecayRate] DEFAULT (0.002);
IF COL_LENGTH('{table}', 'MutualRespectDiversitySensitivity') IS NULL
    ALTER TABLE [{table}] ADD [MutualRespectDiversitySensitivity] float NOT NULL CONSTRAINT [DF_{table}_MutualRespectDiversitySensitivity] DEFAULT (0.40);
IF COL_LENGTH('{table}', 'MutualRespectChallengeSensitivity') IS NULL
    ALTER TABLE [{table}] ADD [MutualRespectChallengeSensitivity] float NOT NULL CONSTRAINT [DF_{table}_MutualRespectChallengeSensitivity] DEFAULT (0.40);
IF COL_LENGTH('{table}', 'MutualRespectBridgeSensitivity') IS NULL
    ALTER TABLE [{table}] ADD [MutualRespectBridgeSensitivity] float NOT NULL CONSTRAINT [DF_{table}_MutualRespectBridgeSensitivity] DEFAULT (0.40);
IF COL_LENGTH('{table}', 'MutualRespectPopularityPenalty') IS NULL
    ALTER TABLE [{table}] ADD [MutualRespectPopularityPenalty] float NOT NULL CONSTRAINT [DF_{table}_MutualRespectPopularityPenalty] DEFAULT (0.30);");
        }

        private static void AddIntellectualRespectColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.Sql($@"
IF COL_LENGTH('{table}', 'EnableIntellectualRespect') IS NULL
    ALTER TABLE [{table}] ADD [EnableIntellectualRespect] bit NOT NULL CONSTRAINT [DF_{table}_EnableIntellectualRespect] DEFAULT (1);
IF COL_LENGTH('{table}', 'IntellectualRespectBase') IS NULL
    ALTER TABLE [{table}] ADD [IntellectualRespectBase] float NOT NULL CONSTRAINT [DF_{table}_IntellectualRespectBase] DEFAULT (0.30);
IF COL_LENGTH('{table}', 'IntellectualRespectGrowthRate') IS NULL
    ALTER TABLE [{table}] ADD [IntellectualRespectGrowthRate] float NOT NULL CONSTRAINT [DF_{table}_IntellectualRespectGrowthRate] DEFAULT (0.04);
IF COL_LENGTH('{table}', 'IntellectualRespectDecayRate') IS NULL
    ALTER TABLE [{table}] ADD [IntellectualRespectDecayRate] float NOT NULL CONSTRAINT [DF_{table}_IntellectualRespectDecayRate] DEFAULT (0.002);
IF COL_LENGTH('{table}', 'IntellectualRespectDiversitySensitivity') IS NULL
    ALTER TABLE [{table}] ADD [IntellectualRespectDiversitySensitivity] float NOT NULL CONSTRAINT [DF_{table}_IntellectualRespectDiversitySensitivity] DEFAULT (0.50);
IF COL_LENGTH('{table}', 'IntellectualRespectChallengeSensitivity') IS NULL
    ALTER TABLE [{table}] ADD [IntellectualRespectChallengeSensitivity] float NOT NULL CONSTRAINT [DF_{table}_IntellectualRespectChallengeSensitivity] DEFAULT (0.50);
IF COL_LENGTH('{table}', 'IntellectualRespectMentorshipSensitivity') IS NULL
    ALTER TABLE [{table}] ADD [IntellectualRespectMentorshipSensitivity] float NOT NULL CONSTRAINT [DF_{table}_IntellectualRespectMentorshipSensitivity] DEFAULT (0.50);
IF COL_LENGTH('{table}', 'IntellectualRespectEgoPenalty') IS NULL
    ALTER TABLE [{table}] ADD [IntellectualRespectEgoPenalty] float NOT NULL CONSTRAINT [DF_{table}_IntellectualRespectEgoPenalty] DEFAULT (0.30);
IF COL_LENGTH('{table}', 'IntellectualRespectHierarchyPenalty') IS NULL
    ALTER TABLE [{table}] ADD [IntellectualRespectHierarchyPenalty] float NOT NULL CONSTRAINT [DF_{table}_IntellectualRespectHierarchyPenalty] DEFAULT (0.20);");
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
