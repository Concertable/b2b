using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class ObligationChecker : IObligationChecker
{
    // Booking states committing no money — erasing a subject while a booking sits in any of these breaks no
    // settlement. Every other state is a blocking obligation, so a future lifecycle state defaults to "blocking"
    // until it is deliberately classified here.
    private static readonly BookingState[] SettledStates = [BookingState.Cancelled];

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
            .AnyAsync(b => !SettledStates.Contains(b.State), ct);
    }
}
