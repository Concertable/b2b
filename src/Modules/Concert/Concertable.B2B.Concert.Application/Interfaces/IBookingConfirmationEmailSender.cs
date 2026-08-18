using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingConfirmationEmailSender
{
    Task SendAsync(
        ConfirmedBooking booking,
        string venueName,
        string artistName,
        CancellationToken ct = default);
}
