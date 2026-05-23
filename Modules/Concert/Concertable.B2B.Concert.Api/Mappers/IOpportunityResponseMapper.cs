using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.Contracts;

namespace Concertable.B2B.Concert.Api.Mappers;

internal interface IOpportunityResponseMapper
{
    OpportunityResponse ToResponse(OpportunityDto dto);
    IEnumerable<OpportunityResponse> ToResponses(IEnumerable<OpportunityDto> dtos);
    IPagination<OpportunityResponse> ToResponses(IPagination<OpportunityDto> page);
}
