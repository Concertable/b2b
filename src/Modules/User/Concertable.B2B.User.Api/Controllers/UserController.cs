using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.User.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
internal class UserController : ControllerBase
{
    private readonly IUserService userService;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;

    public UserController(IUserService userService, ICurrentUser currentUser, IUserModule userModule)
    {
        this.userService = userService;
        this.currentUser = currentUser;
        this.userModule = userModule;
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
        return Ok(user);
    }
}
