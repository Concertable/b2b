using Concertable.B2B.Tenant.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

/// <summary>A membership joined to its tenant's persona + legal name — everything request-scoped authority needs
/// (active tenant, role, persona) plus the label/persona the switcher lists. <see cref="InvitedByUserId"/> is null
/// for the founding Owner, which orders first for the transitional owner-claim default.</summary>
internal sealed record UserMembership(Guid TenantId, string LegalName, TenantType Type, TenantRole Role, Guid? InvitedByUserId);

internal interface ITenantRepository : IRepository<TenantEntity, Guid>
{
    /// <summary>The caller's membership in a specific tenant — validates an <c>X-Tenant-Id</c> header against
    /// authority. Null = the caller doesn't belong to that tenant (the request then fails closed).</summary>
    Task<UserMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default);

    /// <summary>All of the caller's memberships (unordered) — feeds the single-membership default and the
    /// <c>/me</c> switcher payload.</summary>
    Task<IReadOnlyList<UserMembership>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>The caller's default tenant — founding Owner first, then a stable tie-break — or null if they
    /// have none. Backs the transitional owner claim, which one tenant can't represent for a multi-tenant user
    /// (it dies in Phase 5).</summary>
    Task<Guid?> GetDefaultTenantIdAsync(Guid userId, CancellationToken ct = default);
}
