namespace WorkflowApi.Workflow;

public static class Users
{
    public static readonly IReadOnlyList<User> Data =
    [
        new User { Name = "Paula", Roles = [Roles.Programmer], Division = ApplicationDevelopment },
        new User { Name = "Margaret", Roles = [Roles.Programmer], Division = ApplicationDevelopment },
        new User { Name = "Peter", Roles = [Roles.Manager], Division = ApplicationDevelopment },
        new User { Name = "John", Roles = [Roles.Programmer], Division = DevOps },
        new User { Name = "Emily", Roles = [Roles.Programmer], Division = DevOps },
        new User { Name = "Steven", Roles = [Roles.Manager], Division = DevOps }
    ];

    public static readonly IReadOnlyDictionary<string, User> ByName = Data.ToDictionary(user => user.Name);

    public static IEnumerable<User> GetByTenant(string? tenantId)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        return Data.Where(user => String.Equals(
            NormalizeTenantId(user.TenantId),
            normalizedTenantId,
            StringComparison.Ordinal));
    }

    public static bool IsTenantMatched(string? userName, string? tenantId)
    {
        return !String.IsNullOrWhiteSpace(userName)
               && ByName.TryGetValue(userName, out var user)
               && String.Equals(
                   NormalizeTenantId(user.TenantId),
                   NormalizeTenantId(tenantId),
                   StringComparison.Ordinal);
    }

    private static string? NormalizeTenantId(string? tenantId)
    {
        return String.IsNullOrWhiteSpace(tenantId) ? null : tenantId;
    }

    private const string ApplicationDevelopment = "AppDev";
    private const string DevOps = "DevOps";
}
