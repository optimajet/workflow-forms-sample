using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace WorkflowApi.Data.Migrations;

[Migration(2000013)]
[WorkflowEngineMigration("WorkflowApi.Data.Sql.CreateDocumentAccessDeniedForm.sql")]
public sealed class CreateDocumentAccessDeniedFormMigration : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
