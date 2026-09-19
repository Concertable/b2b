using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingTenantDeletionGuard(
    BookingPrivilegedDbContext context,
    CommandTransactionAccessor transactions) : ITenantDeletionGuard
{
    public async Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Tenant deletion requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        return await context.Bookings.AnyAsync(booking =>
            booking.VenueTenantId == tenantId || booking.ArtistTenantId == tenantId,
            ct);
    }
}
