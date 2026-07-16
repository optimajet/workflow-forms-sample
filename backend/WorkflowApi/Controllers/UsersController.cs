using Microsoft.AspNetCore.Mvc;
using WorkflowLib;

namespace WorkflowApi.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController
{
    [HttpGet("all")]
    public IEnumerable<User> All([FromQuery] string? tenantId)
    {
        return Users.GetByTenant(tenantId);
    }
}
