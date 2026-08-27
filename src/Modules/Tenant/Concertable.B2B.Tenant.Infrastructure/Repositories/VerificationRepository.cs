using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class VerificationRepository(TenantDbContext context)
    : Repository<TenantVerificationEntity>(context), IVerificationRepository
{
    public Task<TenantVerificationEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        Context.Query<TenantVerificationEntity>()
            .Include(v => v.Documents)
            .FirstOrDefaultAsync(v => v.TenantId == tenantId, ct);

    public Task<bool> IsApprovedByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        Context.Query<TenantVerificationEntity>()
            .AnyAsync(v => v.TenantId == tenantId && v.Status == TenantVerificationStatus.Approved, ct);

    public Task<IPagination<PendingVerificationProjection>> GetPendingAsync(IPageParams pageParams) =>
        Context.Query<TenantVerificationEntity>()
            .Where(v => v.Status == TenantVerificationStatus.Pending)
            .OrderBy(v => v.SubmittedAt)
            .Join(
                Context.Query<TenantEntity>(),
                v => v.TenantId,
                t => t.Id,
                (v, t) => new PendingVerificationProjection
                {
                    TenantId = v.TenantId,
                    TenantType = t.Type,
                    SubmittedAt = v.SubmittedAt,
                })
            .ToPaginationAsync(pageParams);
}
