using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

[MessageType("concertable.b2b.concert-application-accepted.v1")]
public record ConcertApplicationAcceptedEvent(
    int LifecycleId,
    int ApplicationId,
    int BookingId) : IIntegrationEvent;
