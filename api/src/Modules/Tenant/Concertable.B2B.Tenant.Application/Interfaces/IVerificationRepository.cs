using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Specifications;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface IVerificationRepository : IRepository<TenantVerificationEntity, Guid>
{
    Task<TenantVerificationEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    Task<TenantVerificationEntity?> GetByTenantIdForReviewAsync(Guid tenantId, CancellationToken ct = default);

    Task<TenantVerificationEntity?> GetByTenantIdAsync(
        Guid tenantId,
        ISpecification<TenantVerificationEntity> spec,
        CancellationToken ct = default);

    Task<bool> IsApprovedByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    Task<IPagination<PendingVerificationProjection>> GetPendingAsync(IPageParams pageParams);
}
