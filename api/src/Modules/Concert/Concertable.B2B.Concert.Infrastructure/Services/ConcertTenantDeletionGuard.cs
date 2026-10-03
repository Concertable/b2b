using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertTenantDeletionGuard(
    ConcertPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor) : ITenantDeletionGuard
{
    public async Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Tenant deletion requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        return await context.Concerts.AnyAsync(concert =>
                   concert.VenueTenantId == tenantId || concert.ArtistTenantId == tenantId,
                   ct)
               || await context.Invoices.AnyAsync(invoice =>
                   invoice.VenueTenantId == tenantId || invoice.ArtistTenantId == tenantId,
                   ct)
               || await context.SelfBillingAgreements.AnyAsync(agreement => agreement.TenantId == tenantId, ct);
    }
}
