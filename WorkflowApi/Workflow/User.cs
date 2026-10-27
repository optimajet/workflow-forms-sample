namespace WorkflowApi.Workflow;

public sealed record User
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> Roles { get; init; }

    public required string Division { get; init; }

    public string? TenantId { get; init; }
}
