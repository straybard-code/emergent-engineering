using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703020000_AddTrustDeltaToAgentActions")]
    public partial class AddTrustDeltaToAgentActions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TrustAfter",
                table: "AgentActions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TrustBefore",
                table: "AgentActions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TrustDelta",
                table: "AgentActions",
                type: "float",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrustAfter",
                table: "AgentActions");

            migrationBuilder.DropColumn(
                name: "TrustBefore",
                table: "AgentActions");

            migrationBuilder.DropColumn(
                name: "TrustDelta",
                table: "AgentActions");
        }
    }
}
