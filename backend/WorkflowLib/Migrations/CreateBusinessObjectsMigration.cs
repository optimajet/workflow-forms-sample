using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace WorkflowLib.Migrations;

[Migration(2000010)]
[WorkflowEngineMigration("WorkflowLib.Sql.CreateBusinessObjects.sql")]
public class CreateBusinessObjectsMigration : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
