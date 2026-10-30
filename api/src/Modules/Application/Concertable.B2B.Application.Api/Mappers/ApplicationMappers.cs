using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Booking.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Application.Api.Mappers;

internal static class ApplicationMappers
{
    extension(ApplicationSummary dto)
    {
        public ApplicationSummaryResponse ToResponse() =>
            new(
                dto.Id,
                dto.Artist,
                new OpportunitySummaryResponse(
                    dto.Opportunity.Id,
                    dto.Opportunity.VenueId,
                    dto.Opportunity.VenueName,
                    dto.Opportunity.StartDate,
                    dto.Opportunity.EndDate,
                    dto.Opportunity.Genres.ToList()),
                ToStatus(dto.Status, dto.BookingStatus, false));
    }

    extension(ApplicationProposal dto)
    {
        public ApplicationProposalResponse ToResponse()
        {
            var checkoutCapable = dto.Opportunity.Deal.DealType.RequiresAcceptCheckout();

            return new ApplicationProposalResponse(
                dto.Id,
                dto.Artist,
                new OpportunityProposalResponse(
                    dto.Opportunity.Id,
                    dto.Opportunity.VenueId,
                    dto.Opportunity.VenueName,
                    dto.Opportunity.StartDate,
                    dto.Opportunity.EndDate,
                    dto.Opportunity.Genres.ToList(),
                    dto.Opportunity.Deal),
                ToStatus(dto.Status, dto.BookingStatus, checkoutCapable),
                new ApplicationActions(
                    Accept: dto.Actions.Accept
                        ? new ActionLink($"/api/application/{dto.Id}/accept", HttpMethods.Post)
                        : null,
                    Checkout: dto.Actions.Checkout
                        ? new ActionLink($"/api/application/{dto.Id}/checkout", HttpMethods.Post)
                        : null,
                    Decline: dto.Actions.Decline
                        ? new ActionLink($"/api/application/{dto.Id}/reject", HttpMethods.Post)
                        : null,
                    Cancel: dto.Actions.Cancel
                        ? new ActionLink($"/api/application/{dto.Id}/cancel", HttpMethods.Post)
                        : null,
                    Withdraw: dto.Actions.Withdraw
                        ? new ActionLink($"/api/application/{dto.Id}/withdraw", HttpMethods.Post)
                        : null,
                    Contract: dto.Actions.Contract
                        ? new ActionLink($"/api/application/{dto.Id}/contract/pdf", HttpMethods.Get)
                        : null));
        }
    }

    extension(IReadOnlyList<ApplicationProposal> proposals)
    {
        public IReadOnlyList<ApplicationProposalResponse> ToResponses() =>
            proposals.Select(proposal => proposal.ToResponse()).ToList();
    }

    private static ApplicationStatus ToStatus(
        ApplicationStatus status,
        BookingStatus? bookingStatus,
        bool checkoutCapable) =>
        bookingStatus switch
        {
            BookingStatus.ConfirmationFailed =>
                ApplicationStatus.AwaitingPayment,
            BookingStatus.AwaitingConfirmation when checkoutCapable =>
                ApplicationStatus.AwaitingPayment,
            BookingStatus.Confirmed or BookingStatus.CancellationPending or BookingStatus.CancellationFailed =>
                ApplicationStatus.Confirmed,
            BookingStatus.Cancelled => ApplicationStatus.Cancelled,
            _ => status
        };
}
