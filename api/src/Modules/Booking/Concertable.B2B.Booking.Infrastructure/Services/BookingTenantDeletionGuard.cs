using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingTenantDeletionGuard(
    BookingPrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor) : ITenantDeletionGuard
{
    public async Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Tenant deletion requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        return await context.Bookings.AnyAsync(booking =>
                   booking.VenueTenantId == tenantId || booking.ArtistTenantId == tenantId,
                   ct)
               || await context.Contracts.AnyAsync(contract =>
                   contract.VenueTenantId == tenantId || contract.ArtistTenantId == tenantId,
                   ct);
    }
}
