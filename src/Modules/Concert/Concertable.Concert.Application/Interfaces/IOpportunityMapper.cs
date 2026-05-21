using Concertable.Concert.Application.DTOs;

namespace Concertable.Concert.Application.Interfaces;

internal interface IOpportunityMapper
{
    Task<OpportunityDto> ToDtoAsync(OpportunityEntity opportunity);
    Task<IEnumerable<OpportunityDto>> ToDtosAsync(IEnumerable<OpportunityEntity> opportunities);
    Task<IPagination<OpportunityDto>> ToDtosAsync(IPagination<OpportunityEntity> opportunities);
}
