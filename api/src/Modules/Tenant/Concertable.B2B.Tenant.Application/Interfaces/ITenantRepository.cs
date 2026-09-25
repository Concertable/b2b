using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantRepository : IRepository<TenantEntity, Guid>
{
    Task<TenantEntity?> GetByIdForAdministrationAsync(Guid tenantId, CancellationToken ct = default);

    Task<TenantEntity?> GetByCreatedByUserIdForCreationAsync(Guid userId, CancellationToken ct = default);

    Task<TenantBusinessDetails?> GetTenantBusinessDetailsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<TenantBusinessDetails>> GetTenantBusinessDetailsByTenantIdsAsync(
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default);

    Task<bool> HasActiveBusinessActivityAsync(
        Guid tenantId,
        TenantBusinessActivityKind kind,
        CancellationToken ct = default);

    Task RemoveBusinessActivitiesByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
