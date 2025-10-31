using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using OptimaJet.Workflow.Core.Runtime;
using OptimaJet.Workflow.Plugins.FormsPlugin;
using WorkflowLib;

namespace WorkflowApi.Controllers;

[ApiController]
[Route("api/reports/forms")]
public class WeeklyReportsFormsController : ControllerBase
{
    private readonly WorkflowRuntime _runtime;

    public WeeklyReportsFormsController(WorkflowRuntimeService workflowRuntimeService)
    {
        _runtime = workflowRuntimeService.Runtime;
    }

    public record SubmitReportApiRequest(string User);

    public record SubmitReportApiResponse(Guid ProcessId);

    [HttpPost]
    [Route("submit")]
    public async Task<ActionResult<SubmitReportApiResponse>> SubmitReport([FromBody] SubmitReportApiRequest request)
    {
        var processId = Guid.NewGuid();
        var createdInstanceParams = new CreateInstanceParams("WeeklyReportProcess", processId) { IdentityId = request.User };
        await _runtime.CreateInstanceAsync(createdInstanceParams);
        return Ok(new SubmitReportApiResponse(processId));
    }

    [HttpGet]
    [Route("form")]
    public async Task<ActionResult<Form>> GetForm([FromQuery] string formName, [FromQuery] int? formVersion)
    {
        GetFormResult formResponse = await _runtime.GetFormsRuntimeApi().GetFormAsync(new GetFormParameters
        {
            FormKey = new FormKey { FormName = formName, FormVersion = formVersion }
        });

        return formResponse.Match<ActionResult>(
            ok => Ok(ok.Form),
            error => Problem(error.Message, statusCode: 500)
        );
    }


    [HttpGet]
    [Route("get")]
    public async Task<ActionResult<ExecutableForm[]>> GetForms([FromQuery] Guid processId, [FromQuery] string user)
    {
        FormsRuntimeApi formsPluginRuntimeApi = _runtime.GetFormsRuntimeApi();
        GetExecutableFormsResult executableFormsResponse = await formsPluginRuntimeApi
            .GetExecutableFormsAsync(new GetExecutableFormsParameters { ProcessId = processId, IdentityId = user, ConditionCheck = true });

        if (executableFormsResponse.IsSystemError)
        {
            return Problem(executableFormsResponse.AsSystemError.Message, statusCode: 500);
        }

        ImmutableList<ExecutableForm> forms = executableFormsResponse.AsSuccess.Forms;

        if (!forms.Any())
        {
            GetProcessFormResult processFormResponse = await formsPluginRuntimeApi.GetProcessFormAsync(new GetProcessFormParameters
            {
                FormKey = new FormKey { FormName = "View" },
                ProcessId = processId,
                DataParameterName = WeeklyReportParameterProvider.ParameterWeeklyProgressReport
            });

            if (processFormResponse.IsSystemError)
            {
                throw new Exception(executableFormsResponse.AsSystemError.Message);
            }

            ProcessForm defaultForm = processFormResponse.AsSuccess.Form;

            return Ok(new[]
            {
                new ExecutableForm
                {
                    ProcessId = processId,
                    FormKey = new FormKey { FormName = "View" },
                    FormCode = defaultForm.FormCode,
                    FormData = defaultForm.FormData.ToCamelCase(),
                    AllowSave = false
                }
            });
        }

        return Ok(forms.Select(f => f with { FormData = f.FormData.ToCamelCase() }).ToArray());
    }

    public record ExecuteFormApiRequest(FormKey FormKey, string CommandName, Guid ProcessId, string User, Dictionary<string, object?> Data);

    public record ExecuteFormApiResponse(bool WasExecuted);

    [HttpPost]
    [Route("execute")]
    public async Task<ActionResult<ExecuteFormApiResponse>> ExecuteForm([FromBody] ExecuteFormApiRequest request)
    {
        Dictionary<string, object?> pascalCaseData = request.Data.ToPascalCase();

        ExecuteFormResult response = await _runtime.GetFormsRuntimeApi()
            .ExecuteFormAsync(
                new ExecuteFormParameters
                {
                    FormKey = request.FormKey,
                    CommandName = request.CommandName,
                    ProcessId = request.ProcessId,
                    IdentityId = request.User,
                    FormData = pascalCaseData
                });

        return response.Match<ActionResult>(
            ok => Ok(new ExecuteFormApiResponse(ok.WasExecuted)),
            validationErrors => BadRequest(validationErrors.Errors.ToCamelCase()),
            error => Problem(error.Message, statusCode: 500)
        );
    }

    public record SaveFormApiRequest(FormKey FormKey, Guid ProcessId, string User, Dictionary<string, object?> Data);

    [HttpPost]
    [Route("save")]
    public async Task<ActionResult<object>> SaveForm([FromBody] SaveFormApiRequest request)
    {
        Dictionary<string, object?> pascalCaseData = request.Data.ToPascalCase();

        SaveFormResult response = await _runtime.GetFormsRuntimeApi().SaveFormAsync(new()
        {
            FormKey = request.FormKey, ProcessId = request.ProcessId, IdentityId = request.User, FormData = pascalCaseData
        });

        return response.Match<ActionResult>(
            ok => Ok(ok.Data.ToCamelCase()),
            validationErrors => BadRequest(validationErrors.Errors.ToCamelCase()),
            error => Problem(error.Message, statusCode: 500)
        );
    }
}
