using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class SelfBillingAgreementGate(ConcertReadDbContext context) : ISelfBillingAgreementGate
{
    public Task<bool> HasCurrentAsync(Guid supplierTenantId, DateTime nowUtc, CancellationToken ct = default) =>
        context.SelfBillingAgreements
            .AnyAsync(a => a.TenantId == supplierTenantId && a.ExpiresAtUtc > nowUtc, ct);
}
