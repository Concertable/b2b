using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantRepository : IRepository<TenantEntity, Guid>
{
    Task<bool> ExistsByIdWithKeyShareLockAsync(Guid tenantId, CancellationToken ct = default);

    Task<TenantEntity?> GetByIdWithUpdateLockAsync(Guid tenantId, CancellationToken ct = default);

    Task<Sha256Digest?> GetSystemRolesFingerprintAsync(CancellationToken ct = default);

    Task<TenantEntity?> GetByCreatedByUserIdForCreationAsync(Guid userId, CancellationToken ct = default);

    Task<TenantBusinessDetails?> GetTenantBusinessDetailsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<TenantBusinessDetails>> GetTenantBusinessDetailsByTenantIdsAsync(
        IReadOnlySet<Guid> tenantIds,
        CancellationToken ct = default);

    Task<bool> HasActiveBusinessActivityAsync(
        Guid tenantId,
        TenantBusinessActivityKind kind,
        CancellationToken ct = default);

    Task RemoveBusinessActivitiesByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
