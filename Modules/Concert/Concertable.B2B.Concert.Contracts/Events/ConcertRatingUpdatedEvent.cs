using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

[MessageType("concertable.b2b.concert-rating-updated.v1")]
public sealed record ConcertRatingUpdatedEvent(int ConcertId, double AverageRating, int ReviewCount) : IIntegrationEvent;
