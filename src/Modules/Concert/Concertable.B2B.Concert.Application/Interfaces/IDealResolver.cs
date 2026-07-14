using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IDealResolver
{
    Task<IDeal> ResolveByOpportunityIdAsync(int opportunityId);
    Task<IDeal> ResolveByApplicationIdAsync(int applicationId);
    Task<IDeal> ResolveByConcertIdAsync(int concertId);
}
