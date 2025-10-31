using FluentMigrator;
using OptimaJet.Workflow.Migrator;

[Migration(2000012)]
[WorkflowEngineMigration("WorkflowLib.Sql.CreateForms.sql")]
public class CreateFormsMigration : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
