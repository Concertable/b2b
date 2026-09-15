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
        public ApplicationResponse ToResponse(TenantType membershipType, BookingSummary? booking) =>
            membershipType switch
            {
                TenantType.Venue => dto.ToVenueResponse(booking),
                TenantType.Artist => dto.ToArtistResponse(booking),
                _ => throw new ArgumentOutOfRangeException(nameof(membershipType), membershipType, null)
            };

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
                        ? ActionLink.Post($"/api/application/{dto.Id}/accept")
                        : null,
                    Checkout: isPending && dto.Opportunity.Deal.DealType.RequiresAcceptCheckout()
                        ? ActionLink.Post($"/api/application/{dto.Id}/checkout")
                        : null,
                    Decline: isPending
                        ? ActionLink.Post($"/api/application/{dto.Id}/reject")
                        : null,
                    Cancel: isPending && booking is null
                        ? ActionLink.Post($"/api/application/{dto.Id}/cancel")
                        : null,
                    Contract: booking is not null
                        ? ActionLink.Get($"/api/application/{dto.Id}/contract/pdf")
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
                        ? ActionLink.Post($"/api/application/{dto.Id}/withdraw")
                        : null,
                    Contract: booking is not null
                        ? ActionLink.Get($"/api/application/{dto.Id}/contract/pdf")
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
