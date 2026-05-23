using Concertable.Contracts;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

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
