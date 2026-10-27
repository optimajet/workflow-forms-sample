using Microsoft.AspNetCore.Mvc;
using OptimaJet.Workflow.Api;
using WorkflowApi.Workflow;

namespace WorkflowApi.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    [HttpGet("all")]
    public ActionResult<IEnumerable<User>> GetAll(
        [FromHeader(Name = WorkflowApiConstants.TenantIdHeader)] string? tenantId)
    {
        return Ok(Users.GetByTenant(tenantId));
    }
}
