using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

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
