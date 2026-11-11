namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record RoleDto(
    Guid Id,
    string Name,
    long Version,
    bool IsSystemPreset,
    bool IsProtectedOwner,
    bool IsInvitationAssignable,
    IReadOnlyList<RolePermissionDto> Permissions);
