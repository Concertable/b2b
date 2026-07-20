using Concertable.B2B.Tenant.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

/// <summary>
/// Accepting a tenant invitation. Top-level (not under <c>api/organizations</c>) because the accepting caller
/// may not belong to any tenant yet — the invitation is addressed by its id + the caller's email, so it needs
/// no active-tenant resolution (which would fail closed with a 403). <c>[Authorize]</c> only; the email-match
/// check in <see cref="IInvitationService.AcceptInvitationAsync"/> is the real gate.
/// </summary>
[ApiController]
[Route("api/invitations")]
[Authorize]
internal sealed class InvitationsController : ControllerBase
{
    private readonly IInvitationService invitationService;

    public InvitationsController(IInvitationService invitationService)
    {
        this.invitationService = invitationService;
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id)
    {
        await invitationService.AcceptInvitationAsync(id);
        return NoContent();
    }
}
