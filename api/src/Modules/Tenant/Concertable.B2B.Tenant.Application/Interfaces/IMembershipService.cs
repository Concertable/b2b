using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Requests;

namespace Concertable.B2B.Tenant.Application.Interfaces;

/// <summary>
/// Member management for the caller's active tenant. The active tenant is resolved from the request-scoped
/// <c>ITenantContext</c> (never a parameter), matching <c>TenantService</c>. The last-Owner invariant lives
/// here — a membership can't see its peers — and rejects a demote/remove that would leave the tenant ownerless.
/// </summary>
internal interface IMembershipService
{
    Task<Result<IReadOnlyList<MemberDto>, ListMembersError>> ListMembersAsync(CancellationToken ct = default);
    Task<UnitResult<ChangeMemberRolesError>> ChangeRolesAsync(Guid userId, ChangeMemberRolesRequest request, CancellationToken ct = default);
    Task<UnitResult<RemoveMemberError>> RemoveMemberAsync(Guid userId, CancellationToken ct = default);
}
