using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.Application.DTOs;

internal sealed record OpportunitySummary(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlySet<Genre> Genres);
