using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Concert.Application.Interfaces;

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
    Task<Option<Guid>> GetOwnerByIdAsync(int id);
    Task<bool> OwnsOpportunityAsync(int opportunityId);
    Task<bool> OwnsOpportunityByApplicationIdAsync(int applicationId);
}
