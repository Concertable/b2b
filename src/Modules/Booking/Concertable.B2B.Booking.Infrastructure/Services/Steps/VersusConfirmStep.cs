using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class VersusConfirmStep : IConfirmStep
{
    private readonly IBookingService bookings;

    public VersusConfirmStep(IBookingService bookings)
    {
        this.bookings = bookings;
    }

    public async Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default)
    {
        var accepted = (VersusAcceptedApplication)application;
        var booking = await this.bookings.CreateDeferredAsync(accepted, accepted.PaymentMethodId, ct);
        await VerifyPaymentAdvancer.AdvanceAsync(this.bookings, booking.Id, accepted.Verification, ct);
        return booking;
    }
}
