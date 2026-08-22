using Concertable.B2B.Opportunity.Application.DTOs;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityHandoffService
{
    Task<OpportunityHandoffDto?> GetDetailsAsync(
        int opportunityId,
        CancellationToken ct = default);
    Task<bool> TryClaimAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default);
}
