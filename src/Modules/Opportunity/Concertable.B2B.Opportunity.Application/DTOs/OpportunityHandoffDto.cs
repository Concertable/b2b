using Concertable.Contracts;

namespace Concertable.B2B.Opportunity.Application.DTOs;

internal sealed record OpportunityHandoffDto(
    int Id,
    int VenueId,
    Guid TenantId,
    int DealId,
    DateTime Start,
    DateTime End,
    IReadOnlyList<Genre> Genres);
