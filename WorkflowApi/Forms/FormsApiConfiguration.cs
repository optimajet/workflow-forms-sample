using Microsoft.AspNetCore.Http.HttpResults;
using OptimaJet.Workflow.Api;
using OptimaJet.Workflow.Api.Configuration;
using OptimaJet.Workflow.Api.Forms;
using OptimaJet.Workflow.Api.Forms.Models;
using OptimaJet.Workflow.Api.Models;
using OptimaJet.Workflow.Plugins.FormsPlugin;
using WorkflowApi.Data;
using WorkflowApi.Workflow;
using ExecutableForm = OptimaJet.Workflow.Api.Forms.Models.ExecutableForm;
using FormKey = OptimaJet.Workflow.Api.Forms.Models.FormKey;
using RuntimeFormKey = OptimaJet.Workflow.Plugins.FormsPlugin.FormKey;

namespace WorkflowApi.Forms;

internal static class FormsApiConfiguration
{
    public static void Configure(IWorkflowApiBuilder workflow)
    {
        // Form Viewer uses GetForm to load nested templates; the main form comes from GetExecutableForms.
        // Allow only the definitions needed as templates, not every form stored in the database.
        // Otherwise, a client could read any form definition by calling GetForm directly.
        // Returning false from OnAuthorize sends 403 and skips the operation.
        workflow.ConfigureOperation(api => api.Forms.GetForm)
            .OnAuthorize(context => ValueTask.FromResult(
                context.Input.FormKey is { FormName: { } formName }
                && context.Services.GetRequiredService<Configuration>().Forms.AllowedFormNames
                    .Contains(formName, StringComparer.Ordinal)
            ));

        // When GetExecutableForms succeeds but returns no forms, choose a fallback on the server:
        // View for users who may read the report, or DocumentAccessDenied for users denied access.
        // Use OnAfter because loading the fallback may fail: we must be able to return 404 or 500
        // instead of the original 200 response. OnAfterSuccess can only replace the response data.
        workflow.ConfigureOperation(api => api.Forms.GetExecutableForms)
            .OnAfter(AddFallbackAsync);
    }

    private static async ValueTask<IResult> AddFallbackAsync(
        IOperationContext<GetExecutableFormsRequest> context,
        IResult result
    )
    {
        // Results<Ok<...>, NotFound<...>, ...> wraps the actual HTTP result.
        // Remove these wrappers before checking for an empty successful response.
        var unwrapped = result;

        while (unwrapped is INestedHttpResult nested)
        {
            unwrapped = nested.Result;
        }

        // Keep errors and existing forms unchanged. Fallback is only for an empty successful response.
        // Empty Ok results from OnBefore or OnException are also handled here.
        if (unwrapped is not Ok<GetExecutableFormsResponse> { Value.Forms.Count: 0 })
        {
            return result;
        }

        var options = context.Services.GetRequiredService<Configuration>().Forms.ViewFallback;

        // The switch disables both fallback forms. The user must also be known to the sample
        // and belong to the tenant selected for this request. This is not a login check.
        if (!options.Enabled || !Users.IsTenantMatched(context.Input.IdentityId, context.TenantId))
        {
            return result;
        }

        // Use this request's runtime to keep all lookups within the selected tenant.
        var runtime = context.Services.GetHttpContextWorkflowRuntime();
        var process = await runtime.Builder.GetProcessInstanceAsync(context.Input.ProcessId);
        await runtime.PersistenceProvider.FillSystemProcessParametersAsync(process);

        // These fallback forms belong to the WeeklyReport sample, not to other workflow schemes.
        if (process.ProcessScheme.GetSchemeCodeForActionCall() != "WeeklyReportProcess")
        {
            return result;
        }

        var repository = context.Services.GetRequiredService<WeeklyReportRepository>();

        // No available commands does not automatically mean the user may read the report.
        // Authors may read their own reports; managers may read reports from their division.
        // Check the root process because a subprocess belongs to the same report.
        if (!await repository.CanReadAsync(
                process.RootProcessId,
                context.Input.IdentityId!,
                context.TenantId,
                context.CancellationToken
            ))
        {
            // Explain the denial with a form that contains no report data.
            return await CreateDocumentAccessDeniedAsync(context, runtime.GetFormsRuntimeApi());
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        // Access is allowed, so load the read-only View form and its report data through the server SDK.
        // The server chooses the form and data parameter; the browser cannot choose another parameter.
        // This SDK call has no cancellation token, so cancellation is checked before and after it.
        var response = await runtime.GetFormsRuntimeApi().GetProcessFormAsync(new GetProcessFormParameters
        {
            ProcessId = context.Input.ProcessId,
            FormKey = new RuntimeFormKey
            {
                FormName = options.FormKey.FormName,
                FormVersion = options.FormKey.FormVersion
            },
            DataParameterName = WeeklyReportParameterProvider.ParameterName
        });
        context.CancellationToken.ThrowIfCancellationRequested();

        if (response.IsSystemError)
        {
            return CreateFormError(response.AsSystemError);
        }

        var form = response.AsSuccess.Form;
        var pipeline = context.Services.GetRequiredService<CamelCaseFormsApiFormDataPipeline>();
        // Match the camelCase field names expected by the UI. Hide the save button and commands
        // because this form is for viewing only; normal server-side save/execute checks still apply.
        var fallback = new ExecutableForm(
            form.ProcessId,
            new FormKey(form.FormKey.FormName, form.FormKey.FormVersion),
            form.FormCode,
            pipeline.ConvertToClient(form.FormData),
            false,
            []
        );
        // Use the normal forms response so the frontend needs no special fallback logic.
        return TypedResults.Ok(new GetExecutableFormsResponse(
        [
            fallback
        ]));
    }

    private static async ValueTask<IResult> CreateDocumentAccessDeniedAsync(
        IOperationContext<GetExecutableFormsRequest> context,
        FormsRuntimeApi formsRuntimeApi
    )
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        // The migration creates this informational form in the database, where its text can be edited.
        // GetFormAsync loads only its definition. Do not load report data for a user denied access.
        var response = await formsRuntimeApi.GetFormAsync(new GetFormParameters
        {
            FormKey = new RuntimeFormKey { FormName = "DocumentAccessDenied" },
            TenantId = context.TenantId
        });
        context.CancellationToken.ThrowIfCancellationRequested();

        if (response.IsSystemError)
        {
            return CreateFormError(response.AsSystemError);
        }

        var form = response.AsSuccess.Form;
        // Send the definition directly to the viewer, with no data, save button, or commands.
        // No additional public GetForm request or allowlist entry is needed for this form.
        return TypedResults.Ok(new GetExecutableFormsResponse(
        [
            new ExecutableForm(
                context.Input.ProcessId,
                new FormKey(form.FormKey.FormName, form.FormKey.FormVersion),
                form.FormCode,
                [],
                false,
                []
            )
        ]));
    }

    private static IResult CreateFormError(SystemError error)
    {
        // A missing fallback definition is 404; other form-loading errors are 500.
        // Do not hide a loading failure behind a successful empty forms list.
        return error.Code == FormsRuntimeErrorCode.FormNotFound
            ? TypedResults.NotFound(new Error(error.Message, ErrorCode.NotFound))
            : TypedResults.Json(
                new Error(error.Message, ErrorCode.SystemError),
                statusCode: StatusCodes.Status500InternalServerError
            );
    }
}
