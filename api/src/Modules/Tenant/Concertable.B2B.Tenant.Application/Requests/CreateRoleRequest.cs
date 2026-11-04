using Concertable.B2B.Tenant.Application.DTOs;

namespace Concertable.B2B.Tenant.Application.Requests;

internal sealed record CreateRoleRequest(
    string Name,
    bool IsInvitationAssignable,
    IReadOnlyList<RolePermissionDto> Permissions);
