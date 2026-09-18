using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Application.Domain.Lifecycle;

namespace Concertable.B2B.Application.Application.DTOs;

/// <summary>An application together with the side the reading tenant is on, so the caller is shaped by its
/// own relationship to the application rather than by what kind of business it is.</summary>
internal sealed record ApplicationDetailsDto(ApplicationDto Application, ApplicationSide Side);

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
