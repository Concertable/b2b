using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantActivityRepository : IRepository<TenantActivityEntity, Guid>
{
    Task<bool> ExistsAsync(Guid tenantId, string sourceKey, CancellationToken ct = default);
    Task<IReadOnlyList<TenantActivityEntity>> GetRecentAsync(
        Guid tenantId,
        int take,
        CancellationToken ct = default);
}
