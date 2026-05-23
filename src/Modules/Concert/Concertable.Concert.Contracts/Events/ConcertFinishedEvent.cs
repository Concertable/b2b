using Concertable.Messaging.Contracts;

namespace Concertable.Concert.Contracts.Events;

public record ConcertFinishedEvent(
    int LifecycleId,
    int ConcertId) : IIntegrationEvent;
