using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class ConcertCreatedIntegrationEventHandler : IIntegrationEventHandler<ConcertCreatedEvent>
{
    private readonly BookingPrivilegedDbContext context;
    private readonly TimeProvider timeProvider;

    public ConcertCreatedIntegrationEventHandler(BookingPrivilegedDbContext context, TimeProvider timeProvider)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task HandleAsync(
        ConcertCreatedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        var booking = await this.context.Bookings.SingleOrDefaultAsync(booking => booking.ApplicationId == @event.ApplicationId, ct);
        if (booking is null)
            return;

        booking.RecordHandOff(this.timeProvider.GetUtcNow().UtcDateTime);
        await this.context.SaveChangesAsync(ct);
    }
}
