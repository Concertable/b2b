namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record PublishedConcert(
    int Id,
    string Name,
    string About,
    DateTime StartsAt,
    DateTime EndsAt,
    string VenueName,
    string ArtistName,
    decimal Price);
