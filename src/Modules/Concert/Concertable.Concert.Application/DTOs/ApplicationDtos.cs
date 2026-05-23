using Concertable.Artist.Contracts;
using Concertable.Concert.Domain.Enums;

namespace Concertable.Concert.Application.DTOs;

internal record ApplicationDto(
    int Id,
    ArtistSummaryDto Artist,
    OpportunityDto Opportunity,
    ApplicationStatus Status);
