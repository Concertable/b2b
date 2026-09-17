namespace Concertable.B2B.Concert.Application.DTOs;

/// <summary>
/// A concert as the public marketplace sees it. Deliberately separate from every private DTO: there is no
/// door revenue, tickets sold, deal, contract, invoice or grant here to omit by accident.
/// </summary>
internal sealed record PublishedConcert(
    int Id,
    string Name,
    string About,
    DateTime StartsAt,
    DateTime EndsAt,
    string VenueName,
    string ArtistName,
    decimal Price);
