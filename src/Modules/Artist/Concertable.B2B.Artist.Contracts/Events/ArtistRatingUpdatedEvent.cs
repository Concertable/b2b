using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Artist.Contracts.Events;

public record ArtistRatingUpdatedEvent(int ArtistId, double AverageRating, int ReviewCount) : IIntegrationEvent;
