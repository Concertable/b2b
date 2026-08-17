using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Opportunity.Api.Responses;
using Concertable.B2B.Opportunity.Application.DTOs;
using Concertable.B2B.Opportunity.Application.Interfaces;
using Concertable.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Opportunity.Api.Mappers;

internal sealed class OpportunityResponseMapper : IOpportunityResponseMapper
{
    private readonly IApplicationModule applicationModule;

    public OpportunityResponseMapper(IApplicationModule applicationModule)
    {
        this.applicationModule = applicationModule;
    }

    public OpportunityResponse ToResponse(OpportunityDto dto)
    {
        var ct = dto.Deal.DealType;

        var actions = new OpportunityActions(
            Checkout: applicationModule.RequiresApplyCheckout(ct)
                ? new ActionLink($"/api/application/opportunity/{dto.Id}/checkout", HttpMethods.Post)
                : null);

        return new OpportunityResponse(
            dto.Id,
            dto.VenueId,
            dto.Deal,
            dto.StartDate,
            dto.EndDate,
            dto.Genres,
            actions);
    }

    public IEnumerable<OpportunityResponse> ToResponses(IEnumerable<OpportunityDto> dtos) =>
        dtos.Select(ToResponse);

    public IPagination<OpportunityResponse> ToResponses(IPagination<OpportunityDto> page) =>
        page.Map(ToResponse);
}
