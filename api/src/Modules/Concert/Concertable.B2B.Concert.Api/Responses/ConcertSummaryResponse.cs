using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Responses;

internal sealed record ConcertSummaryResponse(
    int Id,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    string VenueName,
    string ArtistName,
    ConcertState State);
