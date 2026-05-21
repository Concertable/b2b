using Concertable.Concert.Api.Responses;
using Concertable.Concert.Application.DTOs;
using Concertable.Concert.Application.Interfaces;
using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Capabilities;
using Concertable.Shared;

namespace Concertable.Concert.Api.Mappers;

internal sealed class OpportunityResponseMapper : IOpportunityResponseMapper
{
    private readonly IConcertWorkflowCapabilityRegistry registry;

    public OpportunityResponseMapper(IConcertWorkflowCapabilityRegistry registry)
        => this.registry = registry;

    public OpportunityResponse ToResponse(OpportunityDto dto)
    {
        var ct = dto.Contract.ContractType;

        var actions = new OpportunityActions(
            Checkout: registry.Has<IAppliesCheckout>(ct)
                ? new ActionLink($"/api/Application/opportunity/{dto.Id}/checkout", "POST")
                : null);

        return new OpportunityResponse(
            dto.Id,
            dto.VenueId,
            dto.Contract,
            dto.StartDate,
            dto.EndDate,
            dto.Genres,
            actions);
    }

    public IEnumerable<OpportunityResponse> ToResponses(IEnumerable<OpportunityDto> dtos) =>
        dtos.Select(ToResponse);

    public IPagination<OpportunityResponse> ToResponses(IPagination<OpportunityDto> page) =>
        new Pagination<OpportunityResponse>(ToResponses(page.Data).ToList(), page.TotalCount, page.PageNumber, page.PageSize);
}
