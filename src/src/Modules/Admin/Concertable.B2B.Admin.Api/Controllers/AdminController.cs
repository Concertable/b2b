using Concertable.B2B.Admin.Api.Authorization;
using Concertable.B2B.Admin.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Admin.Api.Controllers;

[Admin]
[ApiController]
[Route("api/[controller]")]
internal sealed class AdminController : ControllerBase
{
    private readonly IAdminService adminService;

    public AdminController(IAdminService adminService)
    {
        this.adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<AdminOverview>> GetOverview() =>
        Ok(await adminService.GetOverviewAsync());

    [HttpDelete("{sub:guid}")]
    public async Task<IActionResult> RevokeAdmin(Guid sub) =>
        (await adminService.RevokeAdminAsync(sub)).ToNoContentOrProblem();

    [HttpPost("/api/AdminInvitation")]
    public async Task<ActionResult<AdminInvitationDto>> Invite(CreateAdminInvitationRequest request) =>
        (await adminService.InviteAsync(request))
            .ToCreatedOrProblem(_ => "/api/AdminInvitation");

    [HttpDelete("/api/AdminInvitation/{id:guid}")]
    public async Task<IActionResult> RevokeInvitation(Guid id) =>
        (await adminService.RevokeInvitationAsync(id)).ToNoContentOrProblem();
}
