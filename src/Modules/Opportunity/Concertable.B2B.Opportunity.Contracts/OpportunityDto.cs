using Concertable.Contracts.Enums;

namespace Concertable.B2B.Opportunity.Contracts;

public sealed record OpportunityDto(
    int Id,
    int VenueId,
    Guid VenueTenantId,
    int DealId,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlySet<Genre> Genres);
