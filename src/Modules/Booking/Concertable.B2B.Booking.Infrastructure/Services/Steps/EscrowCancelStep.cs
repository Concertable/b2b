using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class EscrowCancelStep : ICancelStep
{
    private readonly IBus bus;

    public EscrowCancelStep(IBus bus)
    {
        this.bus = bus;
    }

    public async Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default)
    {
        if (booking.State == BookingState.ConfirmationFailed)
        {
            if (booking.BeginCancellation().TryGetError(out var beginError))
                throw new InvalidOperationException($"Booking cannot begin cancellation from {beginError.Current}.");
            if (booking.Cancel().TryGetError(out var cancelError))
                throw new InvalidOperationException($"Booking cannot cancel from {cancelError.Current}.");
            return;
        }

        var cancellation = booking.BeginCancellation();
        if (!cancellation.TryGetValue(out var operationId))
            throw new InvalidOperationException($"Booking cannot begin cancellation from {booking.State}.");
        await this.bus.SendAsync(new RefundEscrowCommand(
            operationId,
            booking.Id,
            RefundReasonCodes.RequestedByCustomer), ct);
    }
}
