using Reunion;

namespace Concertable.B2B.Opportunity.Contracts;

public interface IOpportunityModule
{
    Task<Option<OpportunityDetails>> GetDetailsAsync(int opportunityId, CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityDetails>> GetDetailsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<Option<OpportunityDetails>> GetOpenDetailsAsync(int opportunityId, CancellationToken ct = default);
    Task<bool> TryClaimAsync(int opportunityId, Guid venueTenantId, CancellationToken ct = default);
    Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
}
