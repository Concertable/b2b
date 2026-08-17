using Concertable.B2B.Opportunity.Api.Responses;
using Concertable.B2B.Opportunity.Application.DTOs;
using Concertable.Contracts;

namespace Concertable.B2B.Opportunity.Api.Mappers;

internal interface IOpportunityResponseMapper
{
    OpportunityResponse ToResponse(OpportunityDto dto);
    IEnumerable<OpportunityResponse> ToResponses(IEnumerable<OpportunityDto> dtos);
    IPagination<OpportunityResponse> ToResponses(IPagination<OpportunityDto> page);
}
