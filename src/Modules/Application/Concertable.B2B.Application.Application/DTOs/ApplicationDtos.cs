using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Application.Domain.State;

namespace Concertable.B2B.Application.Application.DTOs;

internal sealed record ApplicationDto(
    int Id,
    ArtistSummary Artist,
    OpportunitySummary Opportunity,
    ApplicationStatus Status,
    ApplicationState State);

internal sealed record OpportunitySummary(
    int Id,
    int VenueId,
    string VenueName,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlySet<Genre> Genres,
    DealDto Deal);
