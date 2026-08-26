using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface IVerificationRepository : IRepository<TenantVerificationEntity, Guid>
{
    /// <summary>The tracked verification row for a tenant, with its evidence documents loaded, or null if the
    /// tenant has never submitted — the fail-closed "not verified" state has no row at all.</summary>
    Task<TenantVerificationEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
