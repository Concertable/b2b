using Concertable.B2B.Opportunity.Contracts.Errors;
using Reunion;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Opportunity.Contracts;

public interface IOpportunityModule
{
    Task<Option<OpportunityDto>> GetAsync(int opportunityId, CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityDto>> GetAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<Option<OpportunityDto>> GetOpenAsync(int opportunityId, CancellationToken ct = default);
    Task<UnitResult<FillOpportunityError>> FillAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityDto>> GetOpenByVenueTenantIdAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityDto>> GetRecommendedAsync(
        IReadOnlyCollection<int> excludedOpportunityIds,
        IReadOnlySet<Genre> genres,
        CancellationToken ct = default);
}
