using Concertable.B2B.Opportunity.Application.DTOs;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.Contracts;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityMapper
{
    Task<OpportunityDto> ToDtoAsync(OpportunityEntity opportunity);
    Task<IReadOnlyList<OpportunityDto>> ToDtosAsync(IEnumerable<OpportunityEntity> opportunities);
    Task<IPagination<OpportunityDto>> ToDtosAsync(IPagination<OpportunityEntity> opportunities);
}
