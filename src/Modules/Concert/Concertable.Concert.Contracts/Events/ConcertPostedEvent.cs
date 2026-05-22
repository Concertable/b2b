using Concertable.Messaging;
using Concertable.Shared;

namespace Concertable.Concert.Contracts.Events;

public record ConcertPostedEvent(
    int ConcertId,
    string Name,
    string? Avatar,
    decimal Price,
    DateRange Period,
    DateTime DatePosted,
    double? Latitude,
    double? Longitude,
    IReadOnlyCollection<Genre> Genres) : IIntegrationEvent;
