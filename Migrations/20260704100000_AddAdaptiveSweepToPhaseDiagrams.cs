using System;
using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260704100000_AddAdaptiveSweepToPhaseDiagrams")]
    public partial class AddAdaptiveSweepToPhaseDiagrams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdaptiveReason",
                table: "PhaseDiagrams",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdaptiveSourceType",
                table: "PhaseDiagrams",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAdaptiveSweep",
                table: "PhaseDiagrams",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentPhaseDiagramId",
                table: "PhaseDiagrams",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhaseDiagrams_ParentPhaseDiagramId",
                table: "PhaseDiagrams",
                column: "ParentPhaseDiagramId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhaseDiagrams_PhaseDiagrams_ParentPhaseDiagramId",
                table: "PhaseDiagrams",
                column: "ParentPhaseDiagramId",
                principalTable: "PhaseDiagrams",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhaseDiagrams_PhaseDiagrams_ParentPhaseDiagramId",
                table: "PhaseDiagrams");

            migrationBuilder.DropIndex(
                name: "IX_PhaseDiagrams_ParentPhaseDiagramId",
                table: "PhaseDiagrams");

            migrationBuilder.DropColumn(
                name: "AdaptiveReason",
                table: "PhaseDiagrams");

            migrationBuilder.DropColumn(
                name: "AdaptiveSourceType",
                table: "PhaseDiagrams");

            migrationBuilder.DropColumn(
                name: "IsAdaptiveSweep",
                table: "PhaseDiagrams");

            migrationBuilder.DropColumn(
                name: "ParentPhaseDiagramId",
                table: "PhaseDiagrams");
        }
    }
}
