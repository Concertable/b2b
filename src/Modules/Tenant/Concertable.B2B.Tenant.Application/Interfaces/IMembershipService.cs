using Concertable.B2B.Tenant.Application.Requests;

namespace Concertable.B2B.Tenant.Application.Interfaces;

/// <summary>
/// Member management for the caller's active tenant. The active tenant is resolved from the request-scoped
/// <c>ITenantContext</c> (never a parameter), matching <c>TenantService</c>. The last-Owner invariant lives
/// here — a membership can't see its peers — and rejects a demote/remove that would leave the tenant ownerless.
/// </summary>
internal interface IMembershipService
{
    Task<IReadOnlyList<MemberDto>> ListMembersAsync(CancellationToken ct = default);
    Task ChangeRoleAsync(Guid userId, ChangeMemberRoleRequest request, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid userId, CancellationToken ct = default);
    Task DeleteCurrentTenantAsync(CancellationToken ct = default);
}
