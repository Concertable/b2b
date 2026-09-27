using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Specifications;
using Concertable.Kernel.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class VerificationRepository : Repository<TenantVerificationEntity>, IVerificationRepository
{
    private readonly TenantDbContext context;
    private readonly CommandTransactionAccessor transactions;

    public VerificationRepository(
        TenantDbContext context,
        CommandTransactionAccessor transactions) : base(context)
    {
        this.context = context;
        this.transactions = transactions;
    }

    public Task<TenantVerificationEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        Context.Query<TenantVerificationEntity>()
            .FirstOrDefaultAsync(v => v.TenantId == tenantId, ct);

    public async Task<TenantVerificationEntity?> GetByTenantIdForReviewAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Verification review requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM tenant."Verifications"
             WHERE "TenantId" = {tenantId}
             FOR UPDATE
             """,
            ct);
        return await context.Verifications.SingleOrDefaultAsync(
            verification => verification.TenantId == tenantId,
            ct);
    }

    public Task<TenantVerificationEntity?> GetByTenantIdAsync(
        Guid tenantId,
        ISpecification<TenantVerificationEntity> spec,
        CancellationToken ct = default) =>
        Context.Query<TenantVerificationEntity>()
            .Apply(spec)
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
                    LegalName = t.LegalName,
                    ContactEmail = t.ContactEmail,
                    SubmittedAt = v.SubmittedAt,
                })
            .ToPaginationAsync(pageParams);
}
