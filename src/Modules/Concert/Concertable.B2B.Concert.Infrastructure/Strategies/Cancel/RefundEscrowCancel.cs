using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Kernel.Enums;

namespace Concertable.B2B.Concert.Infrastructure.Strategies;

internal sealed class RefundEscrowCancel : ICancel
{
    private readonly IBus bus;

    public RefundEscrowCancel(IBus bus)
    {
        this.bus = bus;
    }

    public Task CancelAsync(ConcertEntity concert, CancellationToken ct = default)
    {
        var cancellation = concert.BeginCancellation();
        if (!cancellation.TryGetValue(out var operationId))
            throw new InvalidOperationException($"Concert cannot begin cancellation from {concert.State}.");
        return this.bus.SendAsync(new RefundEscrowCommand(
            operationId,
            concert.BookingId,
            RefundReasonCodes.RequestedByCustomer), ct);
    }
}
