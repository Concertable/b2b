using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Mvc;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

[ApiController]
[Route("api/organization")]
internal sealed class OrganizationMembershipController : ControllerBase
{
    private readonly IMembershipService membershipService;
    private readonly IInvitationService invitationService;

    public OrganizationMembershipController(
        IMembershipService membershipService,
        IInvitationService invitationService)
    {
        this.membershipService = membershipService;
        this.invitationService = invitationService;
    }

    [HttpGet("members")]
    [HasPermission(TenantPermission.OperationsViewName)]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> GetMembers() =>
        (await membershipService.ListMembersAsync()).ToOkOrProblem();

    [HttpGet("invitations")]
    [HasPermission(TenantPermission.MembersInviteName)]
    public async Task<ActionResult<IReadOnlyList<InvitationDto>>> GetInvitations() =>
        (await invitationService.ListPendingInvitationsAsync()).ToOkOrProblem();

    [HttpPost("invitations")]
    [HasPermission(TenantPermission.MembersInviteName)]
    public async Task<ActionResult<InvitationDto>> Invite(InviteMemberRequest request)
        => (await invitationService.InviteAsync(request))
            .ToCreatedOrProblem(_ => "/api/organization/invitations");

    [HttpDelete("invitations/{id:guid}")]
    [HasPermission(TenantPermission.MembersInviteName)]
    public async Task<IActionResult> RevokeInvitation(Guid id) =>
        (await invitationService.RevokeInvitationAsync(id)).ToNoContentOrProblem();

    [HttpPut("members/{userId:guid}/roles")]
    [HasPermission(TenantPermission.MembersManageRolesName)]
    public async Task<IActionResult> ChangeRoles(Guid userId, ChangeMemberRolesRequest request) =>
        (await membershipService.ChangeRolesAsync(userId, request)).ToNoContentOrProblem();

    [HttpDelete("members/{userId:guid}")]
    [HasPermission(TenantPermission.MembersRemoveName)]
    public async Task<IActionResult> RemoveMember(Guid userId) =>
        (await membershipService.RemoveMemberAsync(userId)).ToNoContentOrProblem();
}
