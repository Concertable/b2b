using Concertable.Concert.Api.Responses;
using Concertable.Concert.Application.DTOs;
using Concertable.Shared;

namespace Concertable.Concert.Api.Mappers;

internal interface IOpportunityResponseMapper
{
    OpportunityResponse ToResponse(OpportunityDto dto);
    IEnumerable<OpportunityResponse> ToResponses(IEnumerable<OpportunityDto> dtos);
    IPagination<OpportunityResponse> ToResponses(IPagination<OpportunityDto> page);
}
