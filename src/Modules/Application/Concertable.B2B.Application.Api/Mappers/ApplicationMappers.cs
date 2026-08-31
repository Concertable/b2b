using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Booking.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Application.Api.Mappers;

internal static class ApplicationMappers
{
    extension(ApplicationDto dto)
    {
        public ApplicationResponse<VenueApplicationActions> ToVenueResponse(BookingSummary? booking)
        {
            var isPending = dto.State == ApplicationState.Applied;
            var status = booking?.Status == BookingStatus.Cancelled
                ? ApplicationStatus.Cancelled
                : dto.Status;

            return ToResponse(
                dto,
                status,
                new VenueApplicationActions(
                    Accept: isPending
                        ? new ActionLink($"/api/application/{dto.Id}/accept", HttpMethods.Post)
                        : null,
                    Checkout: isPending && dto.Opportunity.Deal.DealType.RequiresAcceptCheckout()
                        ? new ActionLink($"/api/application/{dto.Id}/checkout", HttpMethods.Post)
                        : null,
                    Decline: isPending
                        ? new ActionLink($"/api/application/{dto.Id}/reject", HttpMethods.Post)
                        : null,
                    Cancel: isPending && booking is null
                        ? new ActionLink($"/api/application/{dto.Id}/cancel", HttpMethods.Post)
                        : null,
                    Contract: booking is not null
                        ? new ActionLink($"/api/application/{dto.Id}/contract/pdf", HttpMethods.Get)
                        : null));
        }

        public ApplicationResponse<ArtistApplicationActions> ToArtistResponse(BookingSummary? booking)
        {
            var checkoutCapable = dto.Opportunity.Deal.DealType.RequiresAcceptCheckout();
            var status = booking?.Status switch
            {
                BookingStatus.AwaitingConfirmation or BookingStatus.ConfirmationFailed when checkoutCapable =>
                    ApplicationStatus.AwaitingPayment,
                BookingStatus.Confirmed or BookingStatus.CancellationPending or BookingStatus.CancellationFailed =>
                    ApplicationStatus.Confirmed,
                BookingStatus.Cancelled => ApplicationStatus.Cancelled,
                _ => dto.Status
            };

            return ToResponse(
                dto,
                status,
                new ArtistApplicationActions(
                    Withdraw: dto.State == ApplicationState.Applied
                        ? new ActionLink($"/api/application/{dto.Id}/withdraw", HttpMethods.Post)
                        : null,
                    Contract: booking is not null
                        ? new ActionLink($"/api/application/{dto.Id}/contract/pdf", HttpMethods.Get)
                        : null));
        }
    }

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
                dto.Opportunity.Genres.ToList(),
                dto.Opportunity.Deal),
            status,
            actions);
}
