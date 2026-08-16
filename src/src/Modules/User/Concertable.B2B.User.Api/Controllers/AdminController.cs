using Concertable.B2B.User.Api.Authorization;
using Concertable.B2B.User.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.User.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
internal sealed class AdminController : ControllerBase
{
    private readonly IAdminService adminService;

    public AdminController(IAdminService adminService)
    {
        this.adminService = adminService;
    }

    [Admin]
    [HttpGet]
    public async Task<ActionResult<AdminOverviewDto>> GetOverview() =>
        Ok(await adminService.GetOverviewAsync());

    [Admin]
    [HttpDelete("{sub:guid}")]
    public async Task<IActionResult> RevokeAdmin(Guid sub) =>
        (await adminService.RevokeAdminAsync(sub)).ToNoContentOrProblem();

    [HttpGet("me")]
    public async Task<ActionResult<bool>> Me() =>
        Ok(await adminService.IsCurrentUserAdminAsync());

    [Admin]
    [HttpPost("/api/AdminInvitation")]
    public async Task<ActionResult<AdminInvitationDto>> Invite(CreateAdminInvitationRequest request) =>
        (await adminService.InviteAsync(request))
            .ToCreatedOrProblem(_ => "/api/AdminInvitation");

    [Admin]
    [HttpDelete("/api/AdminInvitation/{id:guid}")]
    public async Task<IActionResult> RevokeInvitation(Guid id) =>
        (await adminService.RevokeInvitationAsync(id)).ToNoContentOrProblem();
}
