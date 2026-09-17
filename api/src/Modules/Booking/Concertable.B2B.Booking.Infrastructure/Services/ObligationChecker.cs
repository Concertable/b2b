using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class ObligationChecker : IObligationChecker
{
    private readonly IBookingReadDbContext context;

    public ObligationChecker(IBookingReadDbContext context)
    {
        this.context = context;
    }

    public async Task<bool> HasLiveAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return false;

        return await context.Bookings
            .Where(b => tenantIds.Contains(b.VenueTenantId) || tenantIds.Contains(b.ArtistTenantId))
            .AnyAsync(
                b => !BookingObligation.SettledStates.Contains(b.State)
                    && !(BookingObligation.SettledOnceHandedOff.Contains(b.State) && b.HandedOffAtUtc != null),
                ct);
    }
}
