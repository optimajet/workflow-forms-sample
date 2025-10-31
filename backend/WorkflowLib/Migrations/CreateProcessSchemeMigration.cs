using FluentMigrator;
using OptimaJet.Workflow.Migrator;

[Migration(2000011)]
[WorkflowEngineMigration("WorkflowLib.Sql.CreateProcessScheme.sql")]
public class CreateProcessSchemeMigration : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
