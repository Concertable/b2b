using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Mvc;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

[ApiController]
[Route("api/organizations")]
internal sealed class TenantMembershipController : ControllerBase
{
    private readonly IMembershipService membershipService;
    private readonly IInvitationService invitationService;

    public TenantMembershipController(IMembershipService membershipService, IInvitationService invitationService)
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
        => (await invitationService.InviteAsync(request))
            .ToCreatedOrProblem(_ => "/api/organizations/invitations");

    [HttpDelete("invitations/{id:guid}")]
    [HasPermission(SharedPermissions.MembersInvite)]
    public async Task<IActionResult> RevokeInvitation(Guid id) =>
        (await invitationService.RevokeInvitationAsync(id)).ToNoContentOrProblem();

    [HttpPut("members/{userId:guid}/role")]
    [HasPermission(SharedPermissions.MembersManageRoles)]
    public async Task<IActionResult> ChangeRole(Guid userId, ChangeMemberRoleRequest request) =>
        (await membershipService.ChangeRoleAsync(userId, request)).ToNoContentOrProblem();

    [HttpDelete("members/{userId:guid}")]
    [HasPermission(SharedPermissions.MembersRemove)]
    public async Task<IActionResult> RemoveMember(Guid userId) =>
        (await membershipService.RemoveMemberAsync(userId)).ToNoContentOrProblem();
}
