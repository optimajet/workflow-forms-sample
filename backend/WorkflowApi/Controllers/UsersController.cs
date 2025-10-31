using Microsoft.AspNetCore.Mvc;
using WorkflowLib;

namespace WorkflowApi.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController
{
    [HttpGet("all")]
    public IEnumerable<User> All()
    {
        return Users.Data;
    }
}
