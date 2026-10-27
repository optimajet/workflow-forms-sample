using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace WorkflowApi.Data.Migrations;

[Migration(2000010)]
[WorkflowEngineMigration("WorkflowApi.Data.Sql.CreateBusinessObjects.sql")]
public sealed class CreateBusinessObjectsMigration : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
