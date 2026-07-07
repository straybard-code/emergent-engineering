using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergentEngineering.Migrations
{
    public partial class AddLogDetailSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Scenarios', 'LogDetailLevel') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Scenarios]
                    ADD [LogDetailLevel] nvarchar(32) NOT NULL CONSTRAINT [DF_Scenarios_LogDetailLevel] DEFAULT 'Full';
                END
                ELSE
                BEGIN
                    ALTER TABLE [dbo].[Scenarios]
                    ALTER COLUMN [LogDetailLevel] nvarchar(32) NOT NULL;
                END

                IF COL_LENGTH('dbo.Scenarios', 'StepLogInterval') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Scenarios]
                    ADD [StepLogInterval] int NOT NULL CONSTRAINT [DF_Scenarios_StepLogInterval] DEFAULT 1;
                END

                IF COL_LENGTH('dbo.Scenarios', 'ActionLogInterval') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Scenarios]
                    ADD [ActionLogInterval] int NOT NULL CONSTRAINT [DF_Scenarios_ActionLogInterval] DEFAULT 1;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Experiments', 'LogDetailLevel') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Experiments]
                    ADD [LogDetailLevel] nvarchar(32) NOT NULL CONSTRAINT [DF_Experiments_LogDetailLevel] DEFAULT 'Full';
                END
                ELSE
                BEGIN
                    ALTER TABLE [dbo].[Experiments]
                    ALTER COLUMN [LogDetailLevel] nvarchar(32) NOT NULL;
                END

                IF COL_LENGTH('dbo.Experiments', 'StepLogInterval') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Experiments]
                    ADD [StepLogInterval] int NOT NULL CONSTRAINT [DF_Experiments_StepLogInterval] DEFAULT 1;
                END

                IF COL_LENGTH('dbo.Experiments', 'ActionLogInterval') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Experiments]
                    ADD [ActionLogInterval] int NOT NULL CONSTRAINT [DF_Experiments_ActionLogInterval] DEFAULT 1;
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.SimulationProjects', 'LogDetailLevel') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[SimulationProjects]
                    ADD [LogDetailLevel] nvarchar(32) NOT NULL CONSTRAINT [DF_SimulationProjects_LogDetailLevel] DEFAULT 'Full';
                END
                ELSE
                BEGIN
                    ALTER TABLE [dbo].[SimulationProjects]
                    ALTER COLUMN [LogDetailLevel] nvarchar(32) NOT NULL;
                END

                IF COL_LENGTH('dbo.SimulationProjects', 'StepLogInterval') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[SimulationProjects]
                    ADD [StepLogInterval] int NOT NULL CONSTRAINT [DF_SimulationProjects_StepLogInterval] DEFAULT 1;
                END

                IF COL_LENGTH('dbo.SimulationProjects', 'ActionLogInterval') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[SimulationProjects]
                    ADD [ActionLogInterval] int NOT NULL CONSTRAINT [DF_SimulationProjects_ActionLogInterval] DEFAULT 1;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.SimulationProjects', 'ActionLogInterval') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[SimulationProjects] DROP COLUMN [ActionLogInterval];
                END

                IF COL_LENGTH('dbo.SimulationProjects', 'StepLogInterval') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[SimulationProjects] DROP COLUMN [StepLogInterval];
                END

                IF COL_LENGTH('dbo.SimulationProjects', 'LogDetailLevel') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[SimulationProjects] DROP COLUMN [LogDetailLevel];
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Experiments', 'ActionLogInterval') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Experiments] DROP COLUMN [ActionLogInterval];
                END

                IF COL_LENGTH('dbo.Experiments', 'StepLogInterval') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Experiments] DROP COLUMN [StepLogInterval];
                END

                IF COL_LENGTH('dbo.Experiments', 'LogDetailLevel') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Experiments] DROP COLUMN [LogDetailLevel];
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Scenarios', 'ActionLogInterval') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Scenarios] DROP COLUMN [ActionLogInterval];
                END

                IF COL_LENGTH('dbo.Scenarios', 'StepLogInterval') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Scenarios] DROP COLUMN [StepLogInterval];
                END

                IF COL_LENGTH('dbo.Scenarios', 'LogDetailLevel') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Scenarios] DROP COLUMN [LogDetailLevel];
                END
                """);
        }
    }
}
