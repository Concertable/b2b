using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

public record ConcertFinishedEvent(
    int LifecycleId,
    int ConcertId) : IIntegrationEvent;
