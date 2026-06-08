using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Artist.Contracts.Events;

[MessageType("concertable.b2b.artist-rating-updated.v1")]
public sealed record ArtistRatingUpdatedEvent(int ArtistId, double AverageRating, int ReviewCount) : IIntegrationEvent;
