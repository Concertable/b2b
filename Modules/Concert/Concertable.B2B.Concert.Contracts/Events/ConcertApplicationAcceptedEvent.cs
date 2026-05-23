using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

public record ConcertApplicationAcceptedEvent(
    int LifecycleId,
    int ApplicationId,
    int BookingId) : IIntegrationEvent;
