using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Kernel.Enums;

namespace Concertable.B2B.Concert.Infrastructure.Strategies;

internal sealed class RefundEscrowCancelStep : ICancelStep
{
    private readonly IBus bus;

    public RefundEscrowCancelStep(IBus bus)
    {
        this.bus = bus;
    }

    public Task CancelAsync(ConcertEntity concert, CancellationToken ct = default)
    {
        var cancellation = concert.BeginCancellation();
        if (!cancellation.TryGetValue(out var operationId))
            throw new InvalidOperationException($"Concert cannot begin cancellation from {concert.State}.");
        return bus.SendAsync(new RefundEscrowCommand(
            operationId,
            PaymentOperationReferences.Escrow(concert.BookingId),
            RefundReasonCodes.RequestedByPayer), ct);
    }
}
