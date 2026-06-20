using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.User.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
internal sealed class UserController : ControllerBase
{
    private readonly IUserService userService;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly ITenantModule tenantModule;

    public UserController(IUserService userService, ICurrentUser currentUser, IUserModule userModule, ITenantModule tenantModule)
    {
        this.userService = userService;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.tenantModule = tenantModule;
    }

    [HttpPut("location")]
    public async Task<ActionResult<UserBase>> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var updatedUser = await userService.SaveLocationAsync(request.Latitude, request.Longitude);
        return Ok(updatedUser);
    }

    [HttpGet("/api/auth/me")]
    public async Task<ActionResult<UserBase>> Me()
    {
        var user = await userModule.GetByIdAsync(currentUser.GetId());
        if (user is null) return Unauthorized();

        var memberships = await tenantModule.GetMembershipsAsync(currentUser.GetId());
        return Ok(user with { Memberships = memberships });
    }
}
