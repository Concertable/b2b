using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

public record ConcertRatingUpdatedEvent(int ConcertId, double AverageRating, int ReviewCount) : IIntegrationEvent;
