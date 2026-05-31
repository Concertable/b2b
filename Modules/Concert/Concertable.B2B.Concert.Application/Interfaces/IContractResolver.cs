using Concertable.B2B.Contract.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IContractResolver
{
    Task<IContract> ResolveByOpportunityIdAsync(int opportunityId);
    Task<IContract> ResolveByApplicationIdAsync(int applicationId);
    Task<IContract> ResolveByConcertIdAsync(int concertId);
}
