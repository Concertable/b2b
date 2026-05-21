using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.Concert.Contracts.Events;

public record ConcertFinishedEvent(
    int LifecycleId,
    int ConcertId) : IIntegrationEvent;
