using EmergentEngineering.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703060000_AddStatusToExperiments")]
    public partial class AddStatusToExperiments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Experiments",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Created");

            migrationBuilder.Sql("""
                UPDATE Experiments
                SET Status = 'Completed'
                WHERE EXISTS (
                    SELECT 1
                    FROM ExperimentRuns
                    WHERE ExperimentRuns.ExperimentId = Experiments.Id
                )
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Experiments");
        }
    }
}
