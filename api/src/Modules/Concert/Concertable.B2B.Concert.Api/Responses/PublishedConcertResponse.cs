using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record PublishedConcertResponse(
    int Id,
    string Name,
    string About,
    DateTime StartsAt,
    DateTime EndsAt,
    string VenueName,
    string ArtistName,
    decimal Price);
