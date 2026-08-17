using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Application.Domain.State;

namespace Concertable.B2B.Application.Application.DTOs;

internal sealed record ApplicationDto(
    int Id,
    ArtistSummary Artist,
    OpportunitySnapshot Opportunity,
    ApplicationStatus Status,
    ApplicationState State);

internal sealed record OpportunitySnapshot(
    int Id,
    DateTime StartDate,
    DateTime EndDate,
    IDeal Deal);
