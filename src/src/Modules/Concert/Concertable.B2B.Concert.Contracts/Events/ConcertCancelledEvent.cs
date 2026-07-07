using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

[MessageType("concertable.b2b.concert-cancelled.v1")]
public sealed record ConcertCancelledEvent(
    int LifecycleId,
    int ConcertId) : IIntegrationEvent;
