using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Runtime;

namespace WorkflowLib;

public class WeeklyReportRuleProvider : IWorkflowRuleProvider
{
    private const string RuleCheckRole = "CheckRole";

    private const string RuleAuthor = "Author";

    private const string RuleCheckDivision = "CheckDivision";

    private readonly List<string> _allRules = [RuleCheckRole, RuleAuthor, RuleCheckDivision];

    public List<string> GetRules(string schemeCode, NamesSearchType namesSearchType)
    {
        return _allRules;
    }

    public bool Check(ProcessInstance processInstance, WorkflowRuntime runtime, string? identityId, string ruleName, string parameter)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> CheckAsync(ProcessInstance processInstance, WorkflowRuntime runtime, string? identityId, string ruleName,
        string parameter, CancellationToken token)
    {
        if (!_allRules.Contains(ruleName) || identityId == null || !Users.UserDict.TryGetValue(identityId, out User? user))
        {
            return false;
        }

        // we check that the identityId satisfies our rule, that is, the user has the role specified in the parameter
        if (ruleName == RuleCheckRole)
        {
            return user.Roles.Contains(parameter);
        }

        WeeklyReportData? progressReport =
            await processInstance.GetParameterAsync<WeeklyReportData>(WeeklyReportParameterProvider.ParameterWeeklyProgressReport);
        if (progressReport == null)
        {
            return false;
        }

        // the user who submitted the report is the same as the user who is checking the report
        if (ruleName == RuleAuthor)
        {
            return progressReport.SubmittedBy == user.Name;
        }

        // the division of the user who submitted the report is same as the division of the user who is checking the report
        if (ruleName == RuleCheckDivision)
        {
            if (!Users.UserDict.TryGetValue(progressReport.SubmittedBy, out User? author))
            {
                return false;
            }

            return user.Division == author.Division;
        }

        return false;
    }

    public IEnumerable<string> GetIdentities(ProcessInstance processInstance, WorkflowRuntime runtime, string ruleName, string parameter)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<string>> GetIdentitiesAsync(ProcessInstance processInstance, WorkflowRuntime runtime, string ruleName,
        string parameter, CancellationToken token)
    {
        //TODO comments
        if (ruleName == RuleCheckRole)
        {
            return Users.Data.Where(u => u.Roles.Contains(parameter)).Select(u => u.Name);
        }

        WeeklyReportData? progressReport =
            await processInstance.GetParameterAsync<WeeklyReportData>(WeeklyReportParameterProvider.ParameterWeeklyProgressReport);
        if (progressReport == null)
        {
            return [];
        }

        if (ruleName == RuleAuthor)
        {
            return [progressReport.SubmittedBy];
        }

        if (ruleName == RuleCheckDivision)
        {
            string division = Users.UserDict[progressReport.SubmittedBy].Division;
            return Users.Data.Where(u => u.Division == division).Select(u => u.Name);
        }

        return [];
    }

    public bool IsCheckAsync(string ruleName, string schemeCode)
    {
        // use the CheckAsync method instead of Check
        return true;
    }

    public bool IsGetIdentitiesAsync(string ruleName, string schemeCode)
    {
        // use the GetIdentitiesAsync method instead of GetIdentities
        return true;
    }
}
