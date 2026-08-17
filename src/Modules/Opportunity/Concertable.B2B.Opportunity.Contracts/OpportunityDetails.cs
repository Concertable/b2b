using Concertable.Contracts.Enums;

namespace Concertable.B2B.Opportunity.Contracts;

public sealed record OpportunityDetails(
    int OpportunityId,
    int VenueId,
    Guid VenueTenantId,
    int DealId,
    DateTime StartDate,
    DateTime EndDate,
    IReadOnlyList<Genre> Genres);
