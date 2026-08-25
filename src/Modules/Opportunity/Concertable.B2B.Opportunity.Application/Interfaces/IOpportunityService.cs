using Concertable.B2B.Opportunity.Application.Errors;
using Concertable.B2B.Opportunity.Application.Requests;
using Concertable.B2B.Opportunity.Contracts.Errors;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityService
{
    Task<Option<OpportunityDto>> GetAsync(
        int opportunityId,
        CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityDto>> GetAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<Option<OpportunityDto>> GetOpenAsync(
        int opportunityId,
        CancellationToken ct = default);
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
    Task<Result<OpportunityDto, OpportunityMutationError>> CreateAsync(OpportunityRequest request);
    Task<UnitResult<OpportunityMutationError>> CreateMultipleAsync(IEnumerable<OpportunityRequest> requests);
    Task<Result<IReadOnlyList<OpportunityDto>, OpportunityMutationError>> UpdateAsync(
        int venueId,
        IEnumerable<OpportunityRequest> desired);
    Task<IPagination<OpportunityDto>> GetActiveByVenueIdAsync(int id, IPageParams pageParams);
    Task<IReadOnlyList<OpportunityDto>> GetActiveByVenueIdAsync(int venueId);
    Task<Result<OpportunityDto, OpportunityError>> GetByIdAsync(int id);
    Task<bool> OwnsOpportunityAsync(int opportunityId);
}
