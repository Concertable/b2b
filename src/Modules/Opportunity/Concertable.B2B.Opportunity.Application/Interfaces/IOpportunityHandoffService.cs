using Concertable.B2B.Opportunity.Application.DTOs;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityHandoffService
{
    Task<OpportunityHandoffDto?> GetDetailsAsync(
        int opportunityId,
        CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityHandoffDto>> GetDetailsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<OpportunityHandoffDto?> GetOpenDetailsAsync(
        int opportunityId,
        CancellationToken ct = default);
    Task<bool> TryClaimAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default);
}
