using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Runtime;
using OptimaJet.Workflow.Plugins.FormsPlugin;

namespace WorkflowLib;

public class WeeklyReportValidator : IFormValidator
{
    public async Task<FormValidationResult> ValidateSaveAsync(FormKey formKey, DynamicParameter formValue, CancellationToken token = default)
    {
        return await ValidateFormAsync(formKey, formValue, token);
    }

    public async Task<FormValidationResult> ValidateExecuteAsync(FormKey formKey, WorkflowCommand command, DynamicParameter formValue,
        CancellationToken token = default)
    {
        return await ValidateFormAsync(formKey, formValue, token);
    }

    private Task<FormValidationResult> ValidateFormAsync(FormKey formKey, DynamicParameter formValue, CancellationToken token = default)
    {
        WeeklyReportData weeklyReport = formValue.ConvertTo<WeeklyReportData>();
        Dictionary<string, object?> errors = new();

        if (string.IsNullOrEmpty(formValue["WhatWasDone"]?.ToString()))
        {
            errors.Add("WhatWasDone", "'What was done' is required");
        }

        if (string.IsNullOrEmpty(formValue["EncounteredProblems"]?.ToString()))
        {
            errors.Add("EncounteredProblems", "'Encountered problems' is required");
        }

        if (weeklyReport.StateName == "Review" && string.IsNullOrEmpty(formValue["ManagerReview"]?.ToString()))
        {
            errors.Add("ManagerReview", "'Manager review' is required");
        }

        if (errors.Any())
        {
            return Task.FromResult(new FormValidationResult { IsValid = false, Errors = errors.ToImmutableDictionary() });
        }

        return Task.FromResult(FormValidationResult.Valid);
    }
}
