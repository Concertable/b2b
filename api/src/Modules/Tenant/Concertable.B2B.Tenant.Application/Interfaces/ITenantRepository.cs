using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantRepository : IRepository<TenantEntity, Guid>
{
    Task<BusinessFacts?> GetBusinessFactsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Business facts for many tenants in one query — list consumers read a page of businesses, and a
    /// per-row lookup would issue one round trip each.</summary>
    Task<IReadOnlyList<BusinessFacts>> GetBusinessFactsByTenantIdsAsync(
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default);

    Task<bool> HasActiveBusinessProfileAsync(
        Guid tenantId,
        TenantBusinessProfileKind kind,
        CancellationToken ct = default);

    /// <summary>Removes the tenant's own business-activity rows. They are restricted, not cascading, because
    /// nothing else may delete them; the tenant's own deletion is the one act that may.</summary>
    Task RemoveBusinessProfilesByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
