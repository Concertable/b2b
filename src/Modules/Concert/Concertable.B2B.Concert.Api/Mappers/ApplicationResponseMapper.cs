using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Concert.Api.Mappers;

internal sealed class ApplicationResponseMapper : IApplicationResponseMapper
{
    private readonly IConcertWorkflowCapabilityRegistry registry;

    public ApplicationResponseMapper(IConcertWorkflowCapabilityRegistry registry)
        => this.registry = registry;

    public ApplicationResponse<VenueApplicationActions> ToVenueResponse(ApplicationDto dto)
    {
        var isPending = dto.State == LifecycleState.Applied;
        var isCancellable = dto.State is LifecycleState.Accepted or LifecycleState.PaymentFailed;

        return ToResponse(
            dto,
            dto.Status,
            new VenueApplicationActions(
                Accept: isPending
                    ? new ActionLink($"/api/Application/{dto.Id}/accept", HttpMethods.Post)
                    : null,
                Checkout: isPending && registry.Has<IAcceptsCheckout>(dto.Opportunity.Deal.DealType)
                    ? new ActionLink($"/api/Application/{dto.Id}/checkout", HttpMethods.Post)
                    : null,
                Decline: isPending ? new ActionLink($"/api/Application/{dto.Id}/reject", HttpMethods.Post) : null,
                Cancel: isCancellable ? new ActionLink($"/api/Application/{dto.Id}/cancel", HttpMethods.Post) : null,
                Contract: ContractAction(dto)));
    }

    public IEnumerable<ApplicationResponse<VenueApplicationActions>> ToVenueResponses(IEnumerable<ApplicationDto> dtos) =>
        dtos.Select(ToVenueResponse);

    public ApplicationResponse<ArtistApplicationActions> ToArtistResponse(ApplicationDto dto)
    {
        var checkoutCapable = registry.Has<IAcceptsCheckout>(dto.Opportunity.Deal.DealType);
        var awaitingPayment = checkoutCapable
            && dto.State is LifecycleState.Accepted or LifecycleState.PaymentFailed;
        var status = dto.State switch
        {
            LifecycleState.Accepted or LifecycleState.PaymentFailed when awaitingPayment => ApplicationStatus.AwaitingPayment,
            LifecycleState.Booked or LifecycleState.AwaitingSettlement or LifecycleState.SettlementFailed or LifecycleState.Complete => ApplicationStatus.Confirmed,
            _ => dto.Status
        };

        var canWithdraw = dto.State is LifecycleState.Applied or LifecycleState.Accepted or LifecycleState.PaymentFailed;
        return ToResponse(
            dto,
            status,
            new ArtistApplicationActions(
                Withdraw: canWithdraw
                    ? new ActionLink($"/api/Application/{dto.Id}/withdraw", HttpMethods.Post)
                    : null,
                Contract: ContractAction(dto)));
    }

    public IEnumerable<ApplicationResponse<ArtistApplicationActions>> ToArtistResponses(IEnumerable<ApplicationDto> dtos) =>
        dtos.Select(ToArtistResponse);

    private static ApplicationResponse<TActions> ToResponse<TActions>(
        ApplicationDto dto,
        ApplicationStatus status,
        TActions actions) =>
        new(
            dto.Id,
            dto.Artist,
            new OpportunitySummaryResponse(
                dto.Opportunity.Id,
                dto.Opportunity.VenueId,
                dto.Opportunity.VenueName,
                dto.Opportunity.StartDate,
                dto.Opportunity.EndDate,
                dto.Opportunity.Genres,
                dto.Opportunity.Deal),
            status,
            actions);

    private static ActionLink? ContractAction(ApplicationDto dto) =>
        dto.ContractId is not null
            ? new ActionLink($"/api/Application/{dto.Id}/contract", HttpMethods.Get)
            : null;
}
