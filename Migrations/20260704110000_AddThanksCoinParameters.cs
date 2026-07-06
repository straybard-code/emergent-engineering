using System;
using EmergentEngineering.Data;
using EmergentEngineering.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260704110000_AddThanksCoinParameters")]
    public partial class AddThanksCoinParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddThanksCoinColumns(migrationBuilder, "Experiments");
            AddThanksCoinColumns(migrationBuilder, "Scenarios");
            AddThanksCoinColumns(migrationBuilder, "SimulationProjects");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropThanksCoinColumns(migrationBuilder, "SimulationProjects");
            DropThanksCoinColumns(migrationBuilder, "Scenarios");
            DropThanksCoinColumns(migrationBuilder, "Experiments");
        }

        private static void AddThanksCoinColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableThanksCoin",
                table: table,
                type: "bit",
                nullable: false,
                defaultValue: ThanksCoinDefaults.EnableThanksCoin);

            migrationBuilder.AddColumn<double>(
                name: "ThanksCoinRate",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: ThanksCoinDefaults.ThanksCoinRate);

            migrationBuilder.AddColumn<double>(
                name: "ThanksCoinRespectGain",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: ThanksCoinDefaults.ThanksCoinRespectGain);

            migrationBuilder.AddColumn<double>(
                name: "ThanksCoinTrustGain",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: ThanksCoinDefaults.ThanksCoinTrustGain);

            migrationBuilder.AddColumn<double>(
                name: "ThanksCoinReconfigurationGain",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: ThanksCoinDefaults.ThanksCoinReconfigurationGain);

            migrationBuilder.AddColumn<double>(
                name: "ThanksCoinPsychologicalSafetyGain",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: ThanksCoinDefaults.ThanksCoinPsychologicalSafetyGain);

            migrationBuilder.AddColumn<double>(
                name: "ThanksCoinBridgeGain",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: ThanksCoinDefaults.ThanksCoinBridgeGain);

            migrationBuilder.AddColumn<double>(
                name: "ThanksCoinPopularityBias",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: ThanksCoinDefaults.ThanksCoinPopularityBias);

            migrationBuilder.AddColumn<double>(
                name: "ThanksCoinDiversityBonus",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: ThanksCoinDefaults.ThanksCoinDiversityBonus);

            migrationBuilder.AddColumn<double>(
                name: "ThanksCoinChallengeBonus",
                table: table,
                type: "float",
                nullable: false,
                defaultValue: ThanksCoinDefaults.ThanksCoinChallengeBonus);
        }

        private static void DropThanksCoinColumns(MigrationBuilder migrationBuilder, string table)
        {
            migrationBuilder.DropColumn(
                name: "EnableThanksCoin",
                table: table);

            migrationBuilder.DropColumn(
                name: "ThanksCoinRate",
                table: table);

            migrationBuilder.DropColumn(
                name: "ThanksCoinRespectGain",
                table: table);

            migrationBuilder.DropColumn(
                name: "ThanksCoinTrustGain",
                table: table);

            migrationBuilder.DropColumn(
                name: "ThanksCoinReconfigurationGain",
                table: table);

            migrationBuilder.DropColumn(
                name: "ThanksCoinPsychologicalSafetyGain",
                table: table);

            migrationBuilder.DropColumn(
                name: "ThanksCoinBridgeGain",
                table: table);

            migrationBuilder.DropColumn(
                name: "ThanksCoinPopularityBias",
                table: table);

            migrationBuilder.DropColumn(
                name: "ThanksCoinDiversityBonus",
                table: table);

            migrationBuilder.DropColumn(
                name: "ThanksCoinChallengeBonus",
                table: table);
        }
    }
}
