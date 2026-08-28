using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Concert.Api.Mappers;

internal sealed class OpportunityMapper : IOpportunityMapper
{
    private readonly IConcertWorkflowCapabilityRegistry registry;

    public OpportunityMapper(IConcertWorkflowCapabilityRegistry registry)
        => this.registry = registry;

    public OpportunityResponse ToResponse(OpportunityDto dto)
    {
        var ct = dto.Deal.DealType;

        var actions = new OpportunityActions(
            Checkout: registry.Has<IAppliesCheckout>(ct)
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
