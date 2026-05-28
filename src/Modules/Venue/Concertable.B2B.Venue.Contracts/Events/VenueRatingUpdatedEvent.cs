using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Venue.Contracts.Events;

[MessageType("concertable.b2b.venue-rating-updated.v1")]
public record VenueRatingUpdatedEvent(int VenueId, double AverageRating, int ReviewCount) : IIntegrationEvent;
