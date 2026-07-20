using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

/// <summary>
/// Member management for the caller's active organization — list / change-role / remove, plus deleting the
/// organization itself. Persona-agnostic (like <see cref="StripeAccountController"/>), so the guard is a
/// per-action <c>[HasPermission]</c> rather than a class-level one, and there is no <c>[TenantPersona]</c>.
/// The active tenant is resolved inside <c>MembershipService</c> from <c>ITenantContext</c>.
/// </summary>
[ApiController]
[Route("api/organizations")]
internal sealed class OrganizationMembersController : ControllerBase
{
    private readonly IMembershipService membershipService;
    private readonly IInvitationService invitationService;

    public OrganizationMembersController(IMembershipService membershipService, IInvitationService invitationService)
    {
        this.membershipService = membershipService;
        this.invitationService = invitationService;
    }

    [HttpGet("members")]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> GetMembers() =>
        Ok(await membershipService.ListMembersAsync());

    [HttpGet("invitations")]
    [HasPermission(SharedPermissions.MembersInvite)]
    public async Task<ActionResult<IReadOnlyList<InvitationDto>>> GetInvitations() =>
        Ok(await invitationService.ListPendingInvitationsAsync());

    [HttpPost("invitations")]
    [HasPermission(SharedPermissions.MembersInvite)]
    public async Task<ActionResult<InvitationDto>> Invite(InviteMemberRequest request)
    {
        var invitation = await invitationService.InviteAsync(request);
        return CreatedAtAction(nameof(GetInvitations), invitation);
    }

    [HttpDelete("invitations/{id:guid}")]
    [HasPermission(SharedPermissions.MembersInvite)]
    public async Task<IActionResult> RevokeInvitation(Guid id)
    {
        await invitationService.RevokeInvitationAsync(id);
        return NoContent();
    }

    [HttpPut("members/{userId:guid}/role")]
    [HasPermission(SharedPermissions.MembersManageRoles)]
    public async Task<IActionResult> ChangeRole(Guid userId, ChangeMemberRoleRequest request)
    {
        await membershipService.ChangeRoleAsync(userId, request);
        return NoContent();
    }

    [HttpDelete("members/{userId:guid}")]
    [HasPermission(SharedPermissions.MembersRemove)]
    public async Task<IActionResult> RemoveMember(Guid userId)
    {
        await membershipService.RemoveMemberAsync(userId);
        return NoContent();
    }

    [HttpDelete]
    [HasPermission(SharedPermissions.TenantDelete)]
    public async Task<IActionResult> DeleteCurrentTenant()
    {
        await membershipService.DeleteCurrentTenantAsync();
        return NoContent();
    }
}
