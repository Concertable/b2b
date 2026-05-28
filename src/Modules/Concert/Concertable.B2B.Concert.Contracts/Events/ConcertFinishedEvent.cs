using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

[MessageType("concertable.b2b.concert-finished.v1")]
public record ConcertFinishedEvent(
    int LifecycleId,
    int ConcertId) : IIntegrationEvent;
