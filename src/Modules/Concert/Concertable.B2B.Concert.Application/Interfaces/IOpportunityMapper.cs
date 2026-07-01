using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IOpportunityMapper
{
    Task<OpportunityDto> ToDtoAsync(OpportunityEntity opportunity);
    Task<IEnumerable<OpportunityDto>> ToDtosAsync(IEnumerable<OpportunityEntity> opportunities);
    Task<IPagination<OpportunityDto>> ToDtosAsync(IPagination<OpportunityEntity> opportunities);
}
