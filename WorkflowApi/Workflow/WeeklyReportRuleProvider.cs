using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Runtime;
using WorkflowApi.Data;

namespace WorkflowApi.Workflow;

public sealed class WeeklyReportRuleProvider : IWorkflowRuleProvider
{
    public List<string> GetRules(string schemeCode, NamesSearchType namesSearchType)
    {
        return [RuleCheckRole, RuleAuthor, RuleCheckDivision];
    }

    public bool Check(
        ProcessInstance processInstance,
        WorkflowRuntime runtime,
        string? identityId,
        string ruleName,
        string parameter)
    {
        throw new NotSupportedException("This sample uses asynchronous rules.");
    }

    public async Task<bool> CheckAsync(
        ProcessInstance processInstance,
        WorkflowRuntime runtime,
        string? identityId,
        string ruleName,
        string parameter,
        CancellationToken token)
    {
        if (!RuleNames.Contains(ruleName)
            || identityId is null
            || !Users.ByName.TryGetValue(identityId, out var user))
        {
            return false;
        }

        if (ruleName == RuleCheckRole)
        {
            return user.Roles.Contains(parameter);
        }

        var report = await processInstance.GetParameterAsync<WeeklyReportData>(
            WeeklyReportParameterProvider.ParameterName);
        if (report is null)
        {
            return false;
        }

        if (ruleName == RuleAuthor)
        {
            return report.SubmittedBy == user.Name;
        }

        return ruleName == RuleCheckDivision
               && Users.ByName.TryGetValue(report.SubmittedBy, out var author)
               && user.Division == author.Division;
    }

    public IEnumerable<string> GetIdentities(
        ProcessInstance processInstance,
        WorkflowRuntime runtime,
        string ruleName,
        string parameter)
    {
        throw new NotSupportedException("This sample uses asynchronous rules.");
    }

    public async Task<IEnumerable<string>> GetIdentitiesAsync(
        ProcessInstance processInstance,
        WorkflowRuntime runtime,
        string ruleName,
        string parameter,
        CancellationToken token)
    {
        if (ruleName == RuleCheckRole)
        {
            return Users.Data.Where(user => user.Roles.Contains(parameter)).Select(user => user.Name);
        }

        var report = await processInstance.GetParameterAsync<WeeklyReportData>(
            WeeklyReportParameterProvider.ParameterName);
        if (report is null)
        {
            return [];
        }

        if (ruleName == RuleAuthor)
        {
            return [report.SubmittedBy];
        }

        if (ruleName == RuleCheckDivision && Users.ByName.TryGetValue(report.SubmittedBy, out var author))
        {
            return Users.Data.Where(user => user.Division == author.Division).Select(user => user.Name);
        }

        return [];
    }

    public bool IsCheckAsync(string ruleName, string schemeCode)
    {
        return true;
    }

    public bool IsGetIdentitiesAsync(string ruleName, string schemeCode)
    {
        return true;
    }

    private static readonly HashSet<string> RuleNames =
    [
        RuleCheckRole,
        RuleAuthor,
        RuleCheckDivision
    ];

    private const string RuleCheckRole = "CheckRole";
    private const string RuleAuthor = "Author";
    private const string RuleCheckDivision = "CheckDivision";
}
