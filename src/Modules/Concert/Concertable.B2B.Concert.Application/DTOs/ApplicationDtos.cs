using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Domain.Enums;

namespace Concertable.B2B.Concert.Application.DTOs;

internal sealed record ApplicationDto(
    int Id,
    ArtistSummaryDto Artist,
    OpportunityDto Opportunity,
    ApplicationStatus Status);
