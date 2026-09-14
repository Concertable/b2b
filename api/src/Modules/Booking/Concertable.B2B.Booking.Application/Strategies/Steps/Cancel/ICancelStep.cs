using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Strategies;

internal interface ICancelStep : IDealStep
{
    Task CancelAsync(BookingEntity booking, CancellationToken ct = default);
}
