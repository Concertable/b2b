using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Strategies;

internal interface IConfirm : IDealStrategy
{
    Task ConfirmAsync(
        BookingEntity booking,
        CancellationToken ct = default);
}
