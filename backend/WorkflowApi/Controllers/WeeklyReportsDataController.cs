using Microsoft.AspNetCore.Mvc;
using OptimaJet.Workflow.Core.Runtime;
using WorkflowLib;

namespace WorkflowApi.Controllers;

[ApiController]
[Route("api/reports/data")]
public class WeeklyReportsDataController : ControllerBase
{
    private readonly WeeklyReportRepository _weeklyReportRepository;
    private readonly WorkflowRuntime _runtime;

    public WeeklyReportsDataController(WeeklyReportRepository weeklyReportRepository, WorkflowRuntimeService workflowRuntimeService)
    {
        _weeklyReportRepository = weeklyReportRepository;
        _runtime = workflowRuntimeService.Runtime;
    }

    [HttpGet]
    [Route("count")]
    public async Task<ActionResult<int>> GetCount([FromQuery] string user)
    {
        return Ok(await _weeklyReportRepository.GetCountAsync(user));
    }

    [HttpGet]
    [Route("query")]
    public async Task<ActionResult<dynamic[]>> GetReports([FromQuery] string user, [FromQuery] int skip, [FromQuery] int take)
    {
        IEnumerable<WeeklyReportData> weeklyReportData = await _weeklyReportRepository.GetAllAsync(user, skip, take);
        return Ok(weeklyReportData.Select(d => d.GetPropertiesAsDictionary().ToCamelCase()));
    }
}
