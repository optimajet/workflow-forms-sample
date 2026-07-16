using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowLib;

public static class Users
{
    private const string ApplicationDevelopment = "AppDev";
    private const string DevOps = "DevOps";

    public static readonly List<User> Data =
    [
        new() { Name = "Paula", Roles = [Roles.Programmer], Division = ApplicationDevelopment },
        new() { Name = "Margaret", Roles = [Roles.Programmer], Division = ApplicationDevelopment },
        new() { Name = "Peter", Roles = [Roles.Manager], Division = ApplicationDevelopment },
        new() { Name = "John", Roles = [Roles.Programmer], Division = DevOps },
        new() { Name = "Emily", Roles = [Roles.Programmer], Division = DevOps },
        new() { Name = "Steven", Roles = [Roles.Manager], Division = DevOps },
    ];

    public static readonly Dictionary<string, User> UserDict = Data.ToDictionary(u => u.Name);

    public static IEnumerable<User> GetByTenant(string? tenantId)
    {
        string? normalizedTenantId = NormalizeTenantId(tenantId);
        return Data.Where(user => string.Equals(NormalizeTenantId(user.TenantId), normalizedTenantId, StringComparison.Ordinal));
    }

    public static bool IsTenantMatched(string? userName, string? tenantId)
    {
        return !string.IsNullOrWhiteSpace(userName) &&
               UserDict.TryGetValue(userName, out User? user) &&
               string.Equals(NormalizeTenantId(user.TenantId), NormalizeTenantId(tenantId), StringComparison.Ordinal);
    }

    public static string? NormalizeTenantId(string? tenantId)
    {
        return string.IsNullOrWhiteSpace(tenantId) ? null : tenantId;
    }
}
