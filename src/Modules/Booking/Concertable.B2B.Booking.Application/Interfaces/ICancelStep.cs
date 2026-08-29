using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface ICancelStep : IDealStrategy
{
    Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default);
}