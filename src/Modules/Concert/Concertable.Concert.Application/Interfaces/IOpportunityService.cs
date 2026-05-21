using Concertable.Concert.Application.DTOs;
using Concertable.Concert.Application.Requests;

namespace Concertable.Concert.Application.Interfaces;

internal interface IOpportunityService
{
    Task<OpportunityDto> CreateAsync(OpportunityRequest request);
    Task CreateMultipleAsync(IEnumerable<OpportunityRequest> requests);
    Task<IEnumerable<OpportunityDto>> UpdateAsync(int venueId, IEnumerable<OpportunityRequest> desired);
    Task<IPagination<OpportunityDto>> GetActiveByVenueIdAsync(int id, IPageParams pageParams);
    Task<IEnumerable<OpportunityDto>> GetActiveByVenueIdAsync(int venueId);
    Task<OpportunityDto> GetByIdAsync(int id);
    Task<Guid?> GetOwnerByIdAsync(int id);
    Task<bool> OwnsOpportunityAsync(int opportunityId);
    Task<bool> OwnsOpportunityByApplicationIdAsync(int applicationId);
}
