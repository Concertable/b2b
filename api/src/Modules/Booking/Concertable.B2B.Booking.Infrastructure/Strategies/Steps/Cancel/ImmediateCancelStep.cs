using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal sealed class ImmediateCancelStep : ICancelStep
{
    public Task CancelAsync(BookingEntity booking, CancellationToken ct = default)
    {
        if (booking.BeginCancellation().TryGetError(out var beginError))
            throw new InvalidOperationException($"Booking cannot begin cancellation from {beginError.Current}.");
        if (booking.Cancel().TryGetError(out var cancelError))
            throw new InvalidOperationException($"Booking cannot cancel from {cancelError.Current}.");
        return Task.CompletedTask;
    }
}
