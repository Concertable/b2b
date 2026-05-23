using Concertable.Messaging.Contracts;

namespace Concertable.Concert.Contracts.Events;

public record ConcertApplicationAcceptedEvent(
    int LifecycleId,
    int ApplicationId,
    int BookingId) : IIntegrationEvent;
