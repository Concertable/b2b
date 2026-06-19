using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantRepository : IRepository<TenantEntity, Guid>
{
    Task<TenantEntity?> GetByCreatedByUserIdAsync(Guid userId, CancellationToken ct = default);
}
