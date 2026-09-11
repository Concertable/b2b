using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class SelfBillingAgreementRepository
    : TenantScopedRepository<SelfBillingAgreementEntity>, ISelfBillingAgreementRepository
{
    private readonly IConcertReadDbContext readContext;

    public SelfBillingAgreementRepository(
        ConcertDbContext context,
        IConcertReadDbContext readContext,
        ITenantContext tenantContext) : base(context, tenantContext)
    {
        this.readContext = readContext;
    }

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
}
