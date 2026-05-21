using Concertable.Artist.Contracts;

namespace Concertable.Concert.Application.DTOs;

internal record ApplicationDto(
    int Id,
    ArtistSummaryDto Artist,
    OpportunityDto Opportunity,
    ApplicationStatus Status);
