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

    public record SubmitReportApiRequest(string User, string? TenantId);

    public record SubmitReportApiResponse(Guid ProcessId);

    [HttpPost]
    [Route("submit")]
    public async Task<ActionResult<SubmitReportApiResponse>> SubmitReport([FromBody] SubmitReportApiRequest request)
    {
        if (!IsUserTenantMatched(request.User, request.TenantId))
        {
            return Forbid();
        }

        var processId = Guid.NewGuid();
        var createdInstanceParams = new CreateInstanceParams("WeeklyReportProcess", processId) { IdentityId = request.User };
        string? tenantId = Users.NormalizeTenantId(request.TenantId);
        if (tenantId is not null)
        {
            createdInstanceParams.TenantId = tenantId;
        }

        await _runtime.CreateInstanceAsync(createdInstanceParams);
        return Ok(new SubmitReportApiResponse(processId));
    }

    [HttpGet]
    [Route("form")]
    public async Task<ActionResult<Form>> GetForm(
        [FromQuery] string formName,
        [FromQuery] int? formVersion,
        [FromQuery] string? tenantId,
        [FromQuery] string? user)
    {
        if (!IsUserTenantMatched(user, tenantId))
        {
            return Forbid();
        }

        GetFormResult formResponse = await _runtime.GetFormsRuntimeApi().GetFormAsync(new GetFormParameters
        {
            FormKey = new FormKey { FormName = formName, FormVersion = formVersion },
            TenantId = Users.NormalizeTenantId(tenantId)
        });

        return formResponse.Match<ActionResult>(
            ok => Ok(ok.Form),
            error => Problem(error.Message, statusCode: 500)
        );
    }


    [HttpGet]
    [Route("get")]
    public async Task<ActionResult<ExecutableForm[]>> GetForms([FromQuery] Guid processId, [FromQuery] string user, [FromQuery] string? tenantId)
    {
        if (!IsUserTenantMatched(user, tenantId))
        {
            return Forbid();
        }

        if (!await IsProcessTenantMatchedAsync(processId, tenantId))
        {
            return NotFound();
        }

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

    public record ExecuteFormApiRequest(
        FormKey FormKey,
        string CommandName,
        Guid ProcessId,
        string User,
        Dictionary<string, object?> Data,
        string? TenantId);

    public record ExecuteFormApiResponse(bool WasExecuted);

    [HttpPost]
    [Route("execute")]
    public async Task<ActionResult<ExecuteFormApiResponse>> ExecuteForm([FromBody] ExecuteFormApiRequest request)
    {
        if (!IsUserTenantMatched(request.User, request.TenantId))
        {
            return Forbid();
        }

        if (!await IsProcessTenantMatchedAsync(request.ProcessId, request.TenantId))
        {
            return NotFound();
        }

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

    public record SaveFormApiRequest(
        FormKey FormKey,
        Guid ProcessId,
        string User,
        Dictionary<string, object?> Data,
        string? TenantId);

    [HttpPost]
    [Route("save")]
    public async Task<ActionResult<object>> SaveForm([FromBody] SaveFormApiRequest request)
    {
        if (!IsUserTenantMatched(request.User, request.TenantId))
        {
            return Forbid();
        }

        if (!await IsProcessTenantMatchedAsync(request.ProcessId, request.TenantId))
        {
            return NotFound();
        }

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

    private async Task<bool> IsProcessTenantMatchedAsync(Guid processId, string? tenantId)
    {
        var processInstance = await _runtime.GetProcessInstanceAndFillProcessParametersAsync(processId);
        return string.Equals(Users.NormalizeTenantId(processInstance.TenantId), Users.NormalizeTenantId(tenantId), StringComparison.Ordinal);
    }

    private static bool IsUserTenantMatched(string? user, string? tenantId)
    {
        return !string.IsNullOrWhiteSpace(user) && Users.IsTenantMatched(user, tenantId);
    }
}
