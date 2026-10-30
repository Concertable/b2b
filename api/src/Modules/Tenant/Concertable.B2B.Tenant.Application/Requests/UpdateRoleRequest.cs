using Concertable.B2B.Tenant.Application.DTOs;

namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record UpdateRoleRequest(
    string Name,
    long ExpectedVersion,
    bool IsInvitationAssignable,
    IReadOnlyList<RolePermissionDto> Permissions);
