using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.Concert.Contracts.Events;

public record ConcertSettledEvent(
    int LifecycleId,
    int ConcertId,
    int BookingId) : IIntegrationEvent;
