using System.Threading.Tasks;
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Runtime;

namespace WorkflowLib;

public sealed class WeeklyReportParameterProvider : IWorkflowExternalParametersProvider
{
    private WeeklyReportRepository _weeklyReportRepository;

    public const string ParameterWeeklyProgressReport = "WeeklyReport";

    public WeeklyReportParameterProvider(WeeklyReportRepository weeklyReportRepository)
    {
        _weeklyReportRepository = weeklyReportRepository;
    }

    public async Task<object> GetExternalParameterAsync(string parameterName, ProcessInstance processInstance)
    {
        if (parameterName != ParameterWeeklyProgressReport)
        {
            throw new System.ArgumentException($"Parameter '{parameterName}' is not supported", nameof(parameterName));
        }

        return await _weeklyReportRepository.GetByIdAsync(processInstance.RootProcessId);
    }

    public async Task SetExternalParameterAsync(string parameterName, object parameterValue, ProcessInstance processInstance)
    {
        if (parameterValue is not WeeklyReportData weeklyReportData)
        {
            throw new System.ArgumentException(
                $"Parameter '{parameterName}' has invalid type '{parameterValue?.GetType()?.Name ?? "null"}' ",
                nameof(parameterValue));
        }

        weeklyReportData.Id = processInstance.RootProcessId;
        await _weeklyReportRepository.UpsertAsync(weeklyReportData);
    }

    public object GetExternalParameter(string parameterName, ProcessInstance processInstance)
    {
        throw new System.NotImplementedException();
    }

    public void SetExternalParameter(string parameterName, object parameterValue, ProcessInstance processInstance)
    {
        throw new System.NotImplementedException();
    }

    public bool IsGetExternalParameterAsync(string parameterName, string schemeCode, ProcessInstance processInstance)
    {
        //we will use async method to get the parameter
        return true;
    }

    public bool IsSetExternalParameterAsync(string parameterName, string schemeCode, ProcessInstance processInstance)
    {
        //we will not use async method to set the parameter
        return true;
    }

    public bool HasExternalParameter(string parameterName, string schemeCode, ProcessInstance processInstance)
    {
        return parameterName == ParameterWeeklyProgressReport;
    }
}
