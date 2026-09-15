using Concertable.B2B.Tenant.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal sealed record UserMembership(Guid TenantId, string LegalName, TenantType Type, TenantRole Role);

internal interface IMembershipRepository : IRepository<TenantMembershipEntity, Guid>
{
    /// <summary>The caller's membership in a specific tenant — validates an <c>X-Tenant-Id</c> header against
    /// authority. Null = the caller doesn't belong to that tenant (the request then fails closed).</summary>
    Task<UserMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default);

    /// <summary>All of the caller's memberships (unordered) — feeds the single-membership default and the
    /// <c>/me</c> switcher payload.</summary>
    Task<IReadOnlyList<UserMembership>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Every membership row of a tenant — the members-management list (mapped to emails via <c>IUserModule</c>).</summary>
    Task<IReadOnlyList<TenantMembershipEntity>> ListMembershipsByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>A single tracked membership row to mutate (change role) or remove; null if the user isn't a member.</summary>
    Task<TenantMembershipEntity?> FindMembershipAsync(Guid tenantId, Guid userId, CancellationToken ct = default);

    /// <summary>Owners currently in the tenant — the last-Owner invariant reads this before a demote/remove.</summary>
    Task<int> CountOwnersAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Whether the user already belongs to the tenant — guards duplicate invitation-accept.</summary>
    Task<bool> IsMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default);

    /// <summary>Every membership row of a user across all tenants — GDPR erasure severs them all.</summary>
    Task<IReadOnlyList<TenantMembershipEntity>> ListMembershipsByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Members currently in the tenant — erasure reads this after a sever to detect a wound-down (member-less) tenant.</summary>
    Task<int> CountMembersAsync(Guid tenantId, CancellationToken ct = default);
}
