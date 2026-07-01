using Concertable.Contracts;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

[MessageType("concertable.b2b.concert-changed.v1")]
public sealed record ConcertChangedEvent(
    int ConcertId,
    string Name,
    string About,
    string? Avatar,
    string? BannerUrl,
    int TotalTickets,
    int AvailableTickets,
    decimal Price,
    DateRange Period,
    DateTime? DatePosted,
    int ArtistId,
    string ArtistName,
    int VenueId,
    string VenueName,
    double Latitude,
    double Longitude,
    IReadOnlyCollection<Genre> Genres,
    Guid PayeeUserId,
    Guid PayeeOwnerId) : IIntegrationEvent;
