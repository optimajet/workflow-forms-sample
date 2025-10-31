using System.Collections.Generic;

namespace WorkflowLib;

public class WorkflowApiConfiguration
{
    public WorkflowApiCorsConfiguration? Cors { get; set; }
    public WorkflowApiRuntimeConfiguration? Workflow { get; set; }
}

public class WorkflowApiCorsConfiguration
{
    public List<string>? Origins { get; set; }
}

public class WorkflowApiRuntimeConfiguration
{
    public string? LicenseKey { get; set; }
    public string? FormsManagerUrl { get; set; }
}
