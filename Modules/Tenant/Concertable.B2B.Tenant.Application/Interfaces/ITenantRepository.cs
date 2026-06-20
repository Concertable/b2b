using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantRepository : IRepository<TenantEntity, Guid>
{
    /// <summary>The caller's membership row — the source of truth for their active tenant. One row per user today.</summary>
    Task<TenantMembershipEntity?> GetMembershipByUserIdAsync(Guid userId, CancellationToken ct = default);
}
