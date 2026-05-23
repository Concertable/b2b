using Concertable.Messaging.Contracts;

namespace Concertable.Venue.Contracts.Events;

public record VenueRatingUpdatedEvent(int VenueId, double AverageRating, int ReviewCount) : IIntegrationEvent;
