using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Runtime;
using WorkflowApi.Data;

namespace WorkflowApi.Workflow;

public sealed class WeeklyReportParameterProvider : IWorkflowExternalParametersProvider
{
    public WeeklyReportParameterProvider(WeeklyReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<object> GetExternalParameterAsync(string parameterName, ProcessInstance processInstance)
    {
        EnsureParameterSupported(parameterName);
        return await _repository.GetByIdAsync(processInstance.RootProcessId);
    }

    public async Task SetExternalParameterAsync(
        string parameterName,
        object parameterValue,
        ProcessInstance processInstance)
    {
        EnsureParameterSupported(parameterName);
        if (parameterValue is not WeeklyReportData report)
        {
            throw new ArgumentException(
                $"Parameter '{parameterName}' has invalid type '{parameterValue?.GetType().Name ?? "null"}'.",
                nameof(parameterValue));
        }

        report.Id = processInstance.RootProcessId;
        await _repository.UpsertAsync(report);
    }

    public object GetExternalParameter(string parameterName, ProcessInstance processInstance)
    {
        throw new NotSupportedException("This sample uses asynchronous external parameters.");
    }

    public void SetExternalParameter(string parameterName, object parameterValue, ProcessInstance processInstance)
    {
        throw new NotSupportedException("This sample uses asynchronous external parameters.");
    }

    public bool IsGetExternalParameterAsync(string parameterName, string schemeCode, ProcessInstance processInstance)
    {
        return true;
    }

    public bool IsSetExternalParameterAsync(string parameterName, string schemeCode, ProcessInstance processInstance)
    {
        return true;
    }

    public bool HasExternalParameter(string parameterName, string schemeCode, ProcessInstance processInstance)
    {
        return parameterName == ParameterName;
    }

    private static void EnsureParameterSupported(string parameterName)
    {
        if (parameterName != ParameterName)
        {
            throw new ArgumentException($"Parameter '{parameterName}' is not supported.", nameof(parameterName));
        }
    }

    public const string ParameterName = "WeeklyReport";
    private readonly WeeklyReportRepository _repository;
}
