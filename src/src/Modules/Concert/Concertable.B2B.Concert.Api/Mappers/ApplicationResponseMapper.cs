using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Domain.Lifecycle;

namespace Concertable.B2B.Concert.Api.Mappers;

internal sealed class ApplicationResponseMapper : IApplicationResponseMapper
{
    private readonly IConcertWorkflowCapabilityRegistry registry;

    public ApplicationResponseMapper(IConcertWorkflowCapabilityRegistry registry)
        => this.registry = registry;

    public ApplicationResponse ToResponse(ApplicationDto dto)
    {
        var ct = dto.Opportunity.Contract.ContractType;
        var isPending = dto.State == LifecycleState.Applied;
        var isCancellable = dto.State is LifecycleState.Accepted or LifecycleState.PaymentFailed;

        var actions = new ApplicationActions(
            Accept: new ActionLink($"/api/Application/{dto.Id}/accept", "POST"),
            Checkout: registry.Has<IAcceptsCheckout>(ct)
                ? new ActionLink($"/api/Application/{dto.Id}/checkout", "POST")
                : null,
            Withdraw: isPending || isCancellable ? new ActionLink($"/api/Application/{dto.Id}/withdraw", "POST") : null,
            Reject: isPending ? new ActionLink($"/api/Application/{dto.Id}/reject", "POST") : null,
            Cancel: isCancellable ? new ActionLink($"/api/Application/{dto.Id}/cancel", "POST") : null);

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
