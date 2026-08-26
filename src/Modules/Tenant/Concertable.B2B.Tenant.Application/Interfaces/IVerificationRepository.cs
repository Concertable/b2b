using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface IVerificationRepository : IRepository<TenantVerificationEntity, Guid>
{
    /// <summary>The tracked verification row for a tenant, with its evidence documents loaded, or null if the
    /// tenant has never submitted — the fail-closed "not verified" state has no row at all.</summary>
    Task<TenantVerificationEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Whether the tenant has a verification row in <see cref="Domain.Enums.TenantVerificationStatus.Approved"/> —
    /// false when no row exists (never submitted) or the row is <c>Pending</c>/<c>Rejected</c>.</summary>
    Task<bool> IsApprovedAsync(Guid tenantId, CancellationToken ct = default);
}
