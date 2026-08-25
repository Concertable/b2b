using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.Contracts;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityReadRepository
{
    Task<OpportunityEntity?> GetByIdAsync(
        int opportunityId,
        CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityEntity>> GetByIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<OpportunityEntity?> GetOpenByIdAsync(
        int opportunityId,
        CancellationToken ct = default);
    Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId, IPageParams pageParams);
    Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId);
    Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityEntity>> GetMatchCandidatesAsync(
        IReadOnlyCollection<int> excludedOpportunityIds,
        IReadOnlySet<Genre> genres,
        CancellationToken ct = default);
}
