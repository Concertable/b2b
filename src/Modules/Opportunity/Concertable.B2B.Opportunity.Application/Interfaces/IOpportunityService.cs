using Concertable.B2B.Opportunity.Application.DTOs;
using Concertable.B2B.Opportunity.Application.Errors;
using Concertable.B2B.Opportunity.Application.Requests;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityService
{
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
