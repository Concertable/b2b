using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IBookingConfirmationEmailSender
{
    Task SendAsync(
        ConfirmedBookingSnapshot booking,
        string venueName,
        string artistName,
        CancellationToken ct = default);
}
