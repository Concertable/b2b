using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class SelfBillingAgreementRepository
    : TenantScopedRepository<SelfBillingAgreementEntity>, ISelfBillingAgreementRepository
{
    private const int TenantAgreementsLockNamespace = 10241004;
    private readonly ConcertDbContext context;
    private readonly IConcertReadDbContext readContext;
    private readonly UnitOfWorkAccessor unitOfWorkAccessor;

    public SelfBillingAgreementRepository(
        ConcertDbContext context,
        IConcertReadDbContext readContext,
        ITenantContext tenantContext,
        UnitOfWorkAccessor unitOfWorkAccessor) : base(context, tenantContext)
    {
        this.context = context;
        this.readContext = readContext;
        this.unitOfWorkAccessor = unitOfWorkAccessor;
    }

    public async Task AcquireTenantAgreementsSharedLockAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnlistAsync(ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock_shared({TenantAgreementsLockNamespace}, hashtext({tenantId.ToString()}))",
            ct);
    }

    public async Task AcquireTenantAgreementsExclusiveLockAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnlistAsync(ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({TenantAgreementsLockNamespace}, hashtext({tenantId.ToString()}))",
            ct);
    }

    public Task<bool> ExistsByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        readContext.SelfBillingAgreements.AnyAsync(agreement => agreement.TenantId == tenantId, ct);

    public Task<bool> ExistsCurrentByTenantIdAsync(
        Guid tenantId,
        DateTime nowUtc,
        CancellationToken ct = default) =>
        readContext.SelfBillingAgreements.AnyAsync(
            agreement => agreement.TenantId == tenantId && agreement.ExpiresAtUtc > nowUtc,
            ct);

    public Task<SelfBillingAgreementEntity?> GetCurrentAsync(DateTime nowUtc, CancellationToken ct = default) =>
        base.CurrentTenant
            .Where(a => a.ExpiresAtUtc > nowUtc)
            .OrderByDescending(a => a.AcceptedAtUtc)
            .FirstOrDefaultAsync(ct);

    public Task<SelfBillingAgreementEntity?> GetLatestAsync(CancellationToken ct = default) =>
        base.CurrentTenant
            .OrderByDescending(a => a.AcceptedAtUtc)
            .FirstOrDefaultAsync(ct);

    private Task EnlistAsync(CancellationToken ct) =>
        (unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("A tenant agreements lock requires a unit of work."))
            .EnlistAsync(context, ct);
}
