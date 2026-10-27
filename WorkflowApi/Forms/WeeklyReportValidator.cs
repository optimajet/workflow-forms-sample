using System.Collections.Immutable;
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Runtime;
using OptimaJet.Workflow.Plugins.FormsPlugin;
using WorkflowApi.Data;

namespace WorkflowApi.Forms;

public sealed class WeeklyReportValidator : IFormValidator
{
    public Task<FormValidationResult> ValidateSaveAsync(
        FormKey formKey,
        DynamicParameter formValue,
        CancellationToken token = default)
    {
        return ValidateAsync(formValue);
    }

    public Task<FormValidationResult> ValidateExecuteAsync(
        FormKey formKey,
        WorkflowCommand command,
        DynamicParameter formValue,
        CancellationToken token = default)
    {
        return ValidateAsync(formValue);
    }

    private static Task<FormValidationResult> ValidateAsync(DynamicParameter formValue)
    {
        var report = formValue.ConvertTo<WeeklyReportData>();
        var errors = new Dictionary<string, object?>();

        AddRequiredError(formValue, errors, "WhatWasDone", "'What was done' is required.");
        AddRequiredError(formValue, errors, "EncounteredProblems", "'Encountered problems' is required.");

        if (report.StateName == "Review")
        {
            AddRequiredError(formValue, errors, "ManagerReview", "'Manager review' is required.");
        }

        return Task.FromResult(errors.Count == 0
            ? FormValidationResult.Valid
            : new FormValidationResult
            {
                IsValid = false,
                Errors = errors.ToImmutableDictionary()
            });
    }

    private static void AddRequiredError(
        DynamicParameter formValue,
        IDictionary<string, object?> errors,
        string key,
        string message)
    {
        if (String.IsNullOrWhiteSpace(formValue[key]?.ToString()))
        {
            errors.Add(key, message);
        }
    }
}
