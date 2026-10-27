using Microsoft.AspNetCore.Mvc;
using OptimaJet.Workflow.Api;
using OptimaJet.Workflow.Api.Forms;
using WorkflowApi.Data;
using WorkflowApi.Workflow;

namespace WorkflowApi.Controllers;

[ApiController]
[Route("api/reports/data")]
public sealed class ReportsController : ControllerBase
{
    public ReportsController(
        WeeklyReportRepository repository,
        CamelCaseFormsApiFormDataPipeline formDataPipeline)
    {
        _repository = repository;
        _formDataPipeline = formDataPipeline;
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCount(
        [FromQuery] string user,
        [FromHeader(Name = WorkflowApiConstants.TenantIdHeader)] string? tenantId)
    {
        if (!Users.IsTenantMatched(user, tenantId))
        {
            return Forbid();
        }

        return Ok(await _repository.GetCountAsync(user, tenantId));
    }

    [HttpGet("query")]
    public async Task<ActionResult<IReadOnlyCollection<object?>>> GetReports(
        [FromQuery] string user,
        [FromQuery] int skip,
        [FromQuery] int take,
        [FromHeader(Name = WorkflowApiConstants.TenantIdHeader)] string? tenantId)
    {
        if (!Users.IsTenantMatched(user, tenantId))
        {
            return Forbid();
        }

        if (skip < 0 || take is < 1 or > 100)
        {
            return BadRequest(new { message = "skip must be non-negative and take must be between 1 and 100." });
        }

        var reports = await _repository.GetAllAsync(user, skip, take, tenantId);
        var result = reports
            .Select(report => _formDataPipeline.ConvertToClient(
                report.GetPropertiesAsDictionary().ToDictionary(item => item.Key, item => item.Value)))
            .ToArray();
        return Ok(result);
    }

    private readonly WeeklyReportRepository _repository;
    private readonly CamelCaseFormsApiFormDataPipeline _formDataPipeline;
}
