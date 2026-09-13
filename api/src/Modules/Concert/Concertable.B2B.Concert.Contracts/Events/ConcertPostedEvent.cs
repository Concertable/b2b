using Concertable.Contracts.Enums;
using Concertable.Kernel.ValueObjects;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

[MessageType("concertable.b2b.concert-posted.v1")]
public sealed record ConcertPostedEvent(
    int ConcertId,
    string Name,
    string? Avatar,
    decimal Price,
    DateRange Period,
    DateTime DatePosted,
    double Latitude,
    double Longitude,
    IReadOnlyCollection<Genre> Genres) : IIntegrationEvent;
