using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Runtime;
using WorkflowApi.Data;

namespace WorkflowApi.Workflow;

public sealed class WeeklyReportActionProvider : IWorkflowActionProvider
{
    public void ExecuteAction(
        string name,
        ProcessInstance processInstance,
        WorkflowRuntime runtime,
        string actionParameter)
    {
        throw new NotSupportedException("This sample uses asynchronous actions.");
    }

    public async Task ExecuteActionAsync(
        string name,
        ProcessInstance processInstance,
        WorkflowRuntime runtime,
        string actionParameter,
        CancellationToken token)
    {
        switch (name)
        {
            case ActionInitReport:
                await InitReportAsync(processInstance);
                break;
            case ActionSetReviewer:
                await SetReviewerAsync(processInstance);
                break;
            default:
                throw new NotSupportedException($"Action '{name}' is not supported.");
        }
    }

    public bool ExecuteCondition(
        string name,
        ProcessInstance processInstance,
        WorkflowRuntime runtime,
        string actionParameter)
    {
        throw new NotSupportedException("This sample uses asynchronous conditions.");
    }

    public Task<bool> ExecuteConditionAsync(
        string name,
        ProcessInstance processInstance,
        WorkflowRuntime runtime,
        string actionParameter,
        CancellationToken token)
    {
        return name == CanDeleteCondition
            ? CanDeleteAsync(processInstance, runtime)
            : throw new NotSupportedException($"Condition '{name}' is not supported.");
    }

    public bool IsActionAsync(string name, string schemeCode)
    {
        return true;
    }

    public bool IsConditionAsync(string name, string schemeCode)
    {
        return true;
    }

    public List<string> GetActions(string schemeCode, NamesSearchType namesSearchType)
    {
        return [ActionInitReport, ActionSetReviewer];
    }

    public List<string> GetConditions(string schemeCode, NamesSearchType namesSearchType)
    {
        return [CanDeleteCondition];
    }

    private static async Task InitReportAsync(ProcessInstance processInstance)
    {
        if (processInstance.IsParameterExisting(WeeklyReportParameterProvider.ParameterName))
        {
            throw new InvalidOperationException("Weekly report is already initialized.");
        }

        var submittedOn = DateTime.UtcNow;
        var report = new WeeklyReportData
        {
            SubmittedBy = processInstance.IdentityId,
            SubmittedOn = submittedOn,
            Name = $"Week {submittedOn.GetWeekOfYear()}, {submittedOn.Year}"
        };
        await processInstance.SetParameterAsync(WeeklyReportParameterProvider.ParameterName, report);
    }

    private static async Task SetReviewerAsync(ProcessInstance processInstance)
    {
        var report = await processInstance.GetParameterAsync<WeeklyReportData>(
            WeeklyReportParameterProvider.ParameterName);
        if (report is null)
        {
            throw new InvalidOperationException("Weekly report is not initialized.");
        }

        report.ReviewedBy = processInstance.IdentityId;
        await processInstance.SetParameterAsync(WeeklyReportParameterProvider.ParameterName, report);
    }

    private static async Task<bool> CanDeleteAsync(ProcessInstance processInstance, WorkflowRuntime runtime)
    {
        if (!processInstance.IsSubprocess)
        {
            return !processInstance.CurrentActivity.IsFinal;
        }

        var rootProcess = await runtime.Builder.GetProcessInstanceAsync(processInstance.RootProcessId);
        await runtime.PersistenceProvider.FillSystemProcessParametersAsync(rootProcess);
        return !rootProcess.CurrentActivity.IsFinal;
    }

    private const string ActionInitReport = "InitReport";
    private const string ActionSetReviewer = "SetReviewer";
    private const string CanDeleteCondition = "CanDelete";
}
