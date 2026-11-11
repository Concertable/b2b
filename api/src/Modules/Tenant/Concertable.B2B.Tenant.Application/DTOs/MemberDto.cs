using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Application.DTOs;

internal sealed record MemberDto(Guid UserId, string Email, IReadOnlyList<RoleSummary> Roles);
