using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record InvitationDto(
    Guid Id,
    string Email,
    IReadOnlyList<RoleSummaryDto> Roles,
    DateTime CreatedAt,
    DateTime ExpiresAt);
