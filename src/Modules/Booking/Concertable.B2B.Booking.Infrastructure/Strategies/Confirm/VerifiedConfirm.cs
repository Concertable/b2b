using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal sealed class VerifiedConfirm : IConfirm
{
    public Task ConfirmAsync(
        BookingEntity booking,
        CancellationToken ct = default) => Task.CompletedTask;
}
