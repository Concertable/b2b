using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.Booking.Contracts;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Application.Api.Mappers;

internal sealed class ApplicationResponseMapper : IApplicationResponseMapper
{
    private readonly IApplicationModule applications;
    private readonly IBookingModule bookings;

    public ApplicationResponseMapper(IApplicationModule applications, IBookingModule bookings)
    {
        this.applications = applications;
        this.bookings = bookings;
    }

    public async Task<ApplicationResponse> ToResponseAsync(ApplicationDto dto)
    {
        var bookingOption = await bookings.GetByApplicationIdAsync(dto.Id);
        bookingOption.TryGetValue(out var booking);
        var isPending = dto.State == ApplicationState.Applied;
        var isCancellable = booking?.Status is
            BookingStatus.AwaitingFinancialConfirmation or BookingStatus.FinancialConfirmationFailed;
        var status = booking?.Status == BookingStatus.Cancelled
            ? ApplicationStatus.Cancelled
            : dto.Status;

        var actions = new ApplicationActions(
            Accept: isPending ? new ActionLink($"/api/application/{dto.Id}/accept", HttpMethods.Post) : null,
            Checkout: isPending && applications.RequiresAcceptCheckout(dto.Opportunity.Deal.DealType)
                ? new ActionLink($"/api/application/{dto.Id}/checkout", HttpMethods.Post)
                : null,
            Withdraw: isPending || isCancellable
                ? new ActionLink($"/api/application/{dto.Id}/withdraw", HttpMethods.Post)
                : null,
            Reject: isPending ? new ActionLink($"/api/application/{dto.Id}/reject", HttpMethods.Post) : null,
            Cancel: isCancellable ? new ActionLink($"/api/application/{dto.Id}/cancel", HttpMethods.Post) : null,
            Contract: booking is not null
                ? new ActionLink($"/api/application/{dto.Id}/contract", HttpMethods.Get)
                : null);

        return new ApplicationResponse(
            dto.Id,
            dto.Artist,
            new OpportunitySummaryResponse(
                dto.Opportunity.Id,
                dto.Opportunity.StartDate,
                dto.Opportunity.EndDate,
                dto.Opportunity.Deal),
            status,
            actions);
    }

    public async Task<IReadOnlyList<ApplicationResponse>> ToResponsesAsync(IEnumerable<ApplicationDto> dtos) =>
        await Task.WhenAll(dtos.Select(ToResponseAsync));
}
