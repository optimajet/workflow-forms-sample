using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace WorkflowLib;

public class WeeklyReportRepository
{
    private readonly string? _connectionString;

    public WeeklyReportRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }

    public async Task<int> GetCountAsync(string user)
    {
        List<string>? usersForQuery = GetUsersForQuery(user);
        var builder = new StringBuilder();
        builder.AppendLine("""
                           SELECT COUNT(*)
                           FROM dbo.WeeklyReport WR
                                INNER JOIN dbo.WorkflowProcessInstance WPI ON WR.Id = WPI.Id
                           """);
        if (usersForQuery is not null)
        {
            builder.AppendLine("""
                               WHERE WR.SubmittedBy IN @Users
                               """);
        }

        var parameters = new DynamicParameters();
        if (usersForQuery is not null)
        {
            parameters.Add("Users", usersForQuery);
        }

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleAsync<int>(builder.ToString(), parameters);
    }

    public async Task<IEnumerable<WeeklyReportData>> GetAllAsync(string user, int skip, int take)
    {
        List<string>? usersForQuery = GetUsersForQuery(user);

        var builder = new StringBuilder();
        builder.AppendLine("""
                           SELECT WR.*, WPI.StateName
                           FROM dbo.WeeklyReport WR
                                INNER JOIN dbo.WorkflowProcessInstance WPI ON WR.Id = WPI.Id
                           """);
        if (usersForQuery is not null)
        {
            builder.AppendLine("""
                               WHERE WR.SubmittedBy IN @Users
                               """);
        }

        builder.AppendLine("""
                           ORDER BY WR.SubmittedOn DESC
                           OFFSET @Skip ROWS
                           FETCH NEXT @Take ROWS ONLY;
                           """);

        var parameters = new DynamicParameters();
        if (usersForQuery is not null)
        {
            parameters.Add("Users", usersForQuery);
        }

        parameters.Add("Skip", skip);
        parameters.Add("Take", take);
        IEnumerable<WeeklyReportData> weeklyReportData;
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<WeeklyReportData>(builder.ToString(), parameters);
    }

    public async Task UpsertAsync(WeeklyReportData weeklyReportData)
    {
        string query = """
                       MERGE INTO dbo.WeeklyReport AS Target
                       USING (VALUES (@Id)) AS Source (Id)
                           ON Target.Id = Source.Id
                       WHEN MATCHED THEN
                           UPDATE SET
                               SubmittedBy = @SubmittedBy,
                               SubmittedOn = @SubmittedOn,
                               Name = @Name,
                               ReviewedBy = @ReviewedBy,
                               Details = @Details,
                               Version = @Version
                       WHEN NOT MATCHED THEN
                           INSERT (Id, SubmittedBy, SubmittedOn, Name, ReviewedBy, Details, Version)
                           VALUES (@Id, @SubmittedBy, @SubmittedOn, @Name, @ReviewedBy, @Details, @Version);
                       """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(query, weeklyReportData);
    }

    public async Task<WeeklyReportData> GetByIdAsync(Guid id)
    {
        string query = """
                       SELECT WR.*, WPI.StateName
                       FROM dbo.WeeklyReport WR
                                INNER JOIN dbo.WorkflowProcessInstance WPI ON WR.Id = WPI.Id
                       WHERE WR.Id = @Id
                       """;
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleAsync<WeeklyReportData>(query, new { Id = id });
    }

    private List<string>? GetUsersForQuery(string user)
    {
        if (!Users.UserDict.TryGetValue(user, out User? selectedUser))
        {
            throw new ArgumentException("User not found");
        }

        return selectedUser.Roles.Contains(Roles.Manager)
            ? Users.Data.Where(u => u.Division == selectedUser.Division).Select(u => u.Name).ToList()
            : [user];
    }
}
