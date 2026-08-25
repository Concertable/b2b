using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Booking.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Application.Api.Mappers;

internal sealed class ApplicationResponseMapper : IApplicationResponseMapper
{
    private readonly IBookingModule bookings;

    public ApplicationResponseMapper(IBookingModule bookings)
    {
        this.bookings = bookings;
    }

    public async Task<ApplicationResponse<VenueApplicationActions>> ToVenueResponseAsync(ApplicationDto dto)
    {
        var bookingOption = await bookings.GetByApplicationIdAsync(dto.Id);
        bookingOption.TryGetValue(out var booking);
        return ToVenueResponse(dto, booking);
    }

    public async Task<IReadOnlyList<ApplicationResponse<VenueApplicationActions>>> ToVenueResponsesAsync(
        IEnumerable<ApplicationDto> dtos)
    {
        var dtoList = dtos.ToList();
        var bookingsByApplicationId = (await bookings.GetByApplicationIdsAsync(
                dtoList.Select(dto => dto.Id).ToArray()))
            .ToDictionary(booking => booking.ApplicationId);
        return dtoList
            .Select(dto => ToVenueResponse(dto, bookingsByApplicationId.GetValueOrDefault(dto.Id)))
            .ToList();
    }

    private ApplicationResponse<VenueApplicationActions> ToVenueResponse(
        ApplicationDto dto,
        BookingSummary? booking)
    {
        var isPending = dto.State == State.Applied;
        var isCancellable = booking?.Status is
            BookingStatus.AwaitingConfirmation or BookingStatus.ConfirmationFailed;
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
                Cancel: isCancellable
                    ? new ActionLink($"/api/booking/{booking!.BookingId}/cancel", HttpMethods.Post)
                    : null,
                Contract: booking is not null
                    ? new ActionLink($"/api/application/{dto.Id}/contract/pdf", HttpMethods.Get)
                    : null));
    }

    public async Task<ApplicationResponse<ArtistApplicationActions>> ToArtistResponseAsync(ApplicationDto dto)
    {
        var bookingOption = await bookings.GetByApplicationIdAsync(dto.Id);
        bookingOption.TryGetValue(out var booking);
        return ToArtistResponse(dto, booking);
    }

    public async Task<IReadOnlyList<ApplicationResponse<ArtistApplicationActions>>> ToArtistResponsesAsync(
        IEnumerable<ApplicationDto> dtos)
    {
        var dtoList = dtos.ToList();
        var bookingsByApplicationId = (await bookings.GetByApplicationIdsAsync(
                dtoList.Select(dto => dto.Id).ToArray()))
            .ToDictionary(booking => booking.ApplicationId);
        return dtoList
            .Select(dto => ToArtistResponse(dto, bookingsByApplicationId.GetValueOrDefault(dto.Id)))
            .ToList();
    }

    private ApplicationResponse<ArtistApplicationActions> ToArtistResponse(
        ApplicationDto dto,
        BookingSummary? booking)
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
                Withdraw: dto.State == State.Applied
                    ? new ActionLink($"/api/application/{dto.Id}/withdraw", HttpMethods.Post)
                    : null,
                Contract: booking is not null
                    ? new ActionLink($"/api/application/{dto.Id}/contract/pdf", HttpMethods.Get)
                    : null));
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
