using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Runtime;

namespace WorkflowLib;

public class WeeklyReportActionProvider : IWorkflowActionProvider
{
    private const string ActionInitReport = "InitReport";
    private const string ActionSetReviewer = "SetReviewer";
    private const string CanDeleteCondition = "CanDelete";

    private readonly Dictionary<string, Func<ProcessInstance, Task>> _actions = new()
    {
        { ActionInitReport, InitReportAsync }, { ActionSetReviewer, SetReviewerAsync }
    };

    private readonly Dictionary<string, Func<ProcessInstance, WorkflowRuntime, Task<bool>>> _conditions = new()
    {
        { CanDeleteCondition, CanDeleteAsync }
    };

    public void ExecuteAction(string name, ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter)
    {
        throw new NotImplementedException();
    }

    public async Task ExecuteActionAsync(string name, ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter,
        CancellationToken token)
    {
        if (!_actions.TryGetValue(name, out Func<ProcessInstance, Task>? action))
        {
            throw new NotImplementedException($"Action '{name}' is not supported");
        }

        await action(processInstance);
    }

    private static async Task InitReportAsync(ProcessInstance processInstance)
    {
        if (processInstance.IsParameterExisting(WeeklyReportParameterProvider.ParameterWeeklyProgressReport))
        {
            throw new InvalidOperationException("Weekly report is already initialized");
        }

        DateTime submittedOn = DateTime.Now;
        var weeklyReport = new WeeklyReportData
        {
            SubmittedBy = processInstance.IdentityId,
            SubmittedOn = submittedOn,
            Name = $"Week {submittedOn.GetWeekOfYear()}, {submittedOn.Year}"
        };
        await processInstance.SetParameterAsync(WeeklyReportParameterProvider.ParameterWeeklyProgressReport, weeklyReport);
    }

    private static async Task SetReviewerAsync(ProcessInstance processInstance)
    {
        WeeklyReportData? weeklyReport =
            await processInstance.GetParameterAsync<WeeklyReportData>(WeeklyReportParameterProvider.ParameterWeeklyProgressReport);
        if (weeklyReport == null)
        {
            throw new InvalidOperationException("Weekly report is not initialized");
        }

        weeklyReport.ReviewedBy = processInstance.IdentityId;
        await processInstance.SetParameterAsync(WeeklyReportParameterProvider.ParameterWeeklyProgressReport, weeklyReport);
    }

    public bool ExecuteCondition(string name, ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExecuteConditionAsync(string name, ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter,
        CancellationToken token)
    {
        if (!_conditions.TryGetValue(name, out Func<ProcessInstance, WorkflowRuntime, Task<bool>>? condition))
        {
            throw new NotImplementedException($"Condition '{name}' is not supported");
        }

        return condition(processInstance, runtime);
    }

    private static async Task<bool> CanDeleteAsync(ProcessInstance processInstance, WorkflowRuntime runtime)
    {
        if (!processInstance.IsSubprocess)
        {
            return !processInstance.CurrentActivity.IsFinal;
        }

        ProcessInstance? rootProcessInstance = await runtime.Builder.GetProcessInstanceAsync(processInstance.RootProcessId);
        await runtime.PersistenceProvider.FillSystemProcessParametersAsync(rootProcessInstance);
        return !rootProcessInstance.CurrentActivity.IsFinal;
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
        return _actions.Keys.ToList();
    }

    public List<string> GetConditions(string schemeCode, NamesSearchType namesSearchType)
    {
        return _conditions.Keys.ToList();
    }
}
