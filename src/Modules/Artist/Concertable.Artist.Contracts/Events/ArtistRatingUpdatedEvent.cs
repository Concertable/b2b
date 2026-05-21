using Concertable.Messaging;

namespace Concertable.Artist.Contracts.Events;

public record ArtistRatingUpdatedEvent(int ArtistId, double AverageRating, int ReviewCount) : IIntegrationEvent;
