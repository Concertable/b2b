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
    private readonly IAdminService adminService;

    public UserController(
        IUserService userService,
        ICurrentUser currentUser,
        IUserModule userModule,
        ITenantModule tenantModule,
        IAdminService adminService)
    {
        this.userService = userService;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.tenantModule = tenantModule;
        this.adminService = adminService;
    }

    [HttpPut("location")]
    public async Task<ActionResult<UserDto>> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        return (await userService.SaveLocationAsync(request.Latitude, request.Longitude)).ToOkOrProblem();
    }

    [HttpGet("/api/auth/me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await userModule.GetByIdAsync(currentUser.GetId());
        if (!user.TryGetValue(out var value))
            return Unauthorized();

        var memberships = await tenantModule.GetMembershipsAsync(currentUser.GetId());
        var isAdmin = await adminService.IsCurrentUserAdminAsync();
        return Ok(value with { Memberships = memberships, IsAdmin = isAdmin });
    }
}
