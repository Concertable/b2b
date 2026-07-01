using Concertable.B2B.Artist.Contracts;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record ApplicationDto(
    int Id,
    ArtistSummary Artist,
    OpportunityDto Opportunity,
    ApplicationStatus Status);
