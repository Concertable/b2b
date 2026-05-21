using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.Concert.Contracts.Events;

public record ReviewSubmittedEvent(
    int ArtistId,
    int VenueId,
    int ConcertId,
    double Stars) : IIntegrationEvent;
