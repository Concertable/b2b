using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class ConcertCreatedIntegrationEventHandler : IIntegrationEventHandler<ConcertCreatedEvent>
{
    private readonly IBookingRepository bookingRepository;
    private readonly TimeProvider timeProvider;

    public ConcertCreatedIntegrationEventHandler(IBookingRepository bookingRepository, TimeProvider timeProvider)
    {
        this.bookingRepository = bookingRepository;
        this.timeProvider = timeProvider;
    }

    public async Task HandleAsync(
        ConcertCreatedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        var booking = await this.bookingRepository.GetByApplicationIdAsync(@event.ApplicationId, ct);
        if (booking is null)
            return;

        booking.RecordHandOff(this.timeProvider.GetUtcNow().UtcDateTime);
        await this.bookingRepository.SaveChangesAsync(ct);
    }
}
