using Concertable.Messaging;

namespace Concertable.Concert.Contracts.Events;

public record ConcertRatingUpdatedEvent(int ConcertId, double AverageRating, int ReviewCount) : IIntegrationEvent;
