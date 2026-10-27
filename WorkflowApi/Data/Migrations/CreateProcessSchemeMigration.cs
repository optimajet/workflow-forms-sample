using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace WorkflowApi.Data.Migrations;

[Migration(2000011)]
[WorkflowEngineMigration("WorkflowApi.Data.Sql.CreateProcessScheme.sql")]
public sealed class CreateProcessSchemeMigration : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
