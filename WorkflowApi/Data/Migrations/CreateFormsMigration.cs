using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace WorkflowApi.Data.Migrations;

[Migration(2000012)]
[WorkflowEngineMigration("WorkflowApi.Data.Sql.CreateForms.sql")]
public sealed class CreateFormsMigration : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
