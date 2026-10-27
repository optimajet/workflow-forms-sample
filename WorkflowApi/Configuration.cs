using OptimaJet.Workflow.Api.Forms.Models;
using OptimaJet.Workflow.Api.Options;
using OptimaJet.Workflow.Core;

namespace WorkflowApi;

public sealed record Configuration
{
    public WorkflowApiCoreOptions WorkflowApiCoreOptions { get; set; } = new();

    public WorkflowTenantCreationOptions WorkflowTenantCreationOptions { get; set; } = new()
    {
        PersistenceProviderId = PersistenceProviderId.Mssql,
        ConnectionString = DefaultConnectionString
    };

    public FormsSampleOptions Forms { get; set; } = new();

    public CorsOptions Cors { get; set; } = new();

    public const string DefaultConnectionString =
        "Server=localhost,41434;Database=workflow_forms;User Id=SA;Password=P@ssw0rd;TrustServerCertificate=True;";
}

public sealed record FormsSampleOptions
{
    public string FormsManagerUrl { get; set; } = "http://localhost:5142/formsManager";

    public string[] AllowedFormNames { get; set; } =
    [
        "T:Header",
        "T:EmployeeFields",
        "T:ManagerFields",
        "View"
    ];

    public ViewFallbackOptions ViewFallback { get; set; } = new();
}

public sealed record ViewFallbackOptions
{
    public bool Enabled { get; set; } = true;

    public FormKey FormKey { get; set; } = new("View");
}

public sealed record CorsOptions
{
    public string[] Origins { get; set; } = ["http://localhost:5142"];
}
