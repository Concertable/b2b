using Concertable.Concert.Application.DTOs;
using Concertable.Concert.Domain.Entities;
using Concertable.Contracts;

namespace Concertable.Concert.Application.Interfaces;

internal interface IOpportunityMapper
{
    Task<OpportunityDto> ToDtoAsync(OpportunityEntity opportunity);
    Task<IEnumerable<OpportunityDto>> ToDtosAsync(IEnumerable<OpportunityEntity> opportunities);
    Task<IPagination<OpportunityDto>> ToDtosAsync(IPagination<OpportunityEntity> opportunities);
}
