using Microsoft.Extensions.Configuration;
using OptimaJet.Workflow.DbPersistence;

namespace WorkflowLib;

public class MsSqlProviderService
{
    public MsSqlProviderService(IConfiguration configRoot)
    {
        Provider = new MSSQLProvider(configRoot.GetConnectionString("Default"));
    }

    public MSSQLProvider Provider { get; private set; }
}
