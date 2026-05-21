using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.Venue.Contracts.Events;

public record VenueChangedEvent(
    int VenueId,
    Guid UserId,
    string Name,
    string About,
    string Avatar,
    string County,
    string Town,
    double Latitude,
    double Longitude) : IIntegrationEvent;
