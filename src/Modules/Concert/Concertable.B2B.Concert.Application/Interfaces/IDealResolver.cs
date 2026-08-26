using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IDealResolver
{
    Task<DealDto> ResolveByOpportunityIdAsync(int opportunityId);
    Task<DealDto> ResolveByApplicationIdAsync(int applicationId);
    Task<DealDto> ResolveByConcertIdAsync(int concertId);
}
