using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Venue.Contracts.Events;

public record VenueRatingUpdatedEvent(int VenueId, double AverageRating, int ReviewCount) : IIntegrationEvent;
