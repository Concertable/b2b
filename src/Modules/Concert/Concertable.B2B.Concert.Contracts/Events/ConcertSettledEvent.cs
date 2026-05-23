using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

public record ConcertSettledEvent(
    int LifecycleId,
    int ConcertId,
    int BookingId) : IIntegrationEvent;
