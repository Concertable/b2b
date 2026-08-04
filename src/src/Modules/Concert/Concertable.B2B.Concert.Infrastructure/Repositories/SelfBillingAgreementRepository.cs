using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class SelfBillingAgreementRepository
    : TenantScopedRepository<SelfBillingAgreementEntity>, ISelfBillingAgreementRepository
{
    public SelfBillingAgreementRepository(ConcertDbContext context, ITenantContext tenant) : base(context, tenant) { }

    public Task<SelfBillingAgreementEntity?> GetCurrentAsync(DateTime nowUtc, CancellationToken ct = default) =>
        base.CurrentTenant
            .Where(a => a.ExpiresAtUtc > nowUtc)
            .OrderByDescending(a => a.AcceptedAtUtc)
            .FirstOrDefaultAsync(ct);
}
