using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

[MessageType("concertable.b2b.concert-settled.v1")]
public record ConcertSettledEvent(
    int LifecycleId,
    int ConcertId,
    int BookingId) : IIntegrationEvent;
