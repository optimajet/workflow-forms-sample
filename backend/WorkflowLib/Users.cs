using System.Collections.Generic;
using System.Linq;

namespace WorkflowLib;

public static class Users
{
    private const string ApplicationDevelopment = "AppDev";
    private const string DevOps = "DevOps";
    private const string Management = "Management";

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
}
