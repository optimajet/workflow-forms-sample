using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using WorkflowApi.Workflow;

namespace WorkflowApi.Data;

public sealed class WeeklyReportRepository
{
    public WeeklyReportRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<int> GetCountAsync(string user, string? tenantId)
    {
        var users = GetUsersForQuery(user);
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var query = new StringBuilder(
            """
            SELECT COUNT(*)
            FROM dbo.WeeklyReport WR
                 INNER JOIN dbo.WorkflowProcessInstance WPI ON WR.Id = WPI.Id

            """);
        AppendFilters(query, normalizedTenantId);

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleAsync<int>(query.ToString(), new
        {
            Users = users,
            TenantId = normalizedTenantId
        });
    }

    public async Task<IReadOnlyCollection<WeeklyReportData>> GetAllAsync(
        string user,
        int skip,
        int take,
        string? tenantId)
    {
        var users = GetUsersForQuery(user);
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var query = new StringBuilder(
            """
            SELECT WR.*, WPI.StateName
            FROM dbo.WeeklyReport WR
                 INNER JOIN dbo.WorkflowProcessInstance WPI ON WR.Id = WPI.Id

            """);
        AppendFilters(query, normalizedTenantId);
        query.AppendLine(
            """
            ORDER BY WR.SubmittedOn DESC
            OFFSET @Skip ROWS
            FETCH NEXT @Take ROWS ONLY;
            """);

        await using var connection = new SqlConnection(_connectionString);
        var reports = await connection.QueryAsync<WeeklyReportData>(query.ToString(), new
        {
            Users = users,
            TenantId = normalizedTenantId,
            Skip = skip,
            Take = take
        });
        return reports.AsList();
    }

    public async Task<bool> CanReadAsync(
        Guid processId,
        string user,
        string? tenantId,
        CancellationToken cancellationToken
    )
    {
        if (!Users.IsTenantMatched(user, tenantId))
        {
            return false;
        }

        var normalizedTenantId = NormalizeTenantId(tenantId);
        var query = new StringBuilder(
            """
            SELECT COUNT(*)
            FROM dbo.WeeklyReport WR
                 INNER JOIN dbo.WorkflowProcessInstance WPI ON WR.Id = WPI.Id

            """
        );
        AppendFilters(query, normalizedTenantId);
        query.AppendLine("AND WR.Id = @ProcessId");

        await using var connection = new SqlConnection(_connectionString);
        var command = new CommandDefinition(
            query.ToString(),
            new { ProcessId = processId, Users = GetUsersForQuery(user), TenantId = normalizedTenantId },
            cancellationToken: cancellationToken
        );
        return await connection.ExecuteScalarAsync<int>(command) != 0;
    }

    public async Task UpsertAsync(WeeklyReportData report)
    {
        const string query =
            """
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
        await connection.ExecuteAsync(query, report);
    }

    public async Task<WeeklyReportData> GetByIdAsync(Guid id)
    {
        const string query =
            """
            SELECT WR.*, WPI.StateName
            FROM dbo.WeeklyReport WR
                 INNER JOIN dbo.WorkflowProcessInstance WPI ON WR.Id = WPI.Id
            WHERE WR.Id = @Id
            """;

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleAsync<WeeklyReportData>(query, new { Id = id });
    }

    private static List<string> GetUsersForQuery(string userName)
    {
        if (!Users.ByName.TryGetValue(userName, out var user))
        {
            throw new ArgumentException("User not found.", nameof(userName));
        }

        return user.Roles.Contains(Roles.Manager)
            ? Users.Data.Where(candidate => candidate.Division == user.Division).Select(candidate => candidate.Name).ToList()
            : [userName];
    }

    private static void AppendFilters(StringBuilder query, string? tenantId)
    {
        query.AppendLine(tenantId is null
            ? "WHERE WR.SubmittedBy IN @Users AND WPI.TenantId IS NULL"
            : "WHERE WR.SubmittedBy IN @Users AND WPI.TenantId = @TenantId");
    }

    private static string? NormalizeTenantId(string? tenantId)
    {
        return String.IsNullOrWhiteSpace(tenantId) ? null : tenantId;
    }

    private readonly string _connectionString;
}
