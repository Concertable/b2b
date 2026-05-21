using Concertable.Concert.Api.Responses;
using Concertable.Concert.Application.DTOs;
using Concertable.Concert.Application.Interfaces;
using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Capabilities;

namespace Concertable.Concert.Api.Mappers;

internal sealed class ApplicationResponseMapper : IApplicationResponseMapper
{
    private readonly IConcertWorkflowCapabilityRegistry registry;

    public ApplicationResponseMapper(IConcertWorkflowCapabilityRegistry registry)
        => this.registry = registry;

    public ApplicationResponse ToResponse(ApplicationDto dto)
    {
        var ct = dto.Opportunity.Contract.ContractType;

        var actions = new ApplicationActions(
            Accept: new ActionLink($"/api/Application/{dto.Id}/accept", "POST"),
            Checkout: registry.Has<IAcceptsCheckout>(ct)
                ? new ActionLink($"/api/Application/{dto.Id}/checkout", "POST")
                : null);

        return new ApplicationResponse(
            dto.Id,
            dto.Artist,
            new OpportunitySummaryResponse(
                dto.Opportunity.Id,
                dto.Opportunity.StartDate,
                dto.Opportunity.EndDate,
                dto.Opportunity.Contract),
            dto.Status,
            actions);
    }

    public IEnumerable<ApplicationResponse> ToResponses(IEnumerable<ApplicationDto> dtos) =>
        dtos.Select(ToResponse);

}
