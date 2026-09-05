using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Strategies;

internal interface IConfirmStep : IDealStep
{
    Task ConfirmAsync(
        BookingEntity booking,
        CancellationToken ct = default);
}
