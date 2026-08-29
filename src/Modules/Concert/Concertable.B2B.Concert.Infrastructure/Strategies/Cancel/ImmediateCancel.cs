using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Strategies;

internal sealed class ImmediateCancel : ICancel
{
    public Task CancelAsync(ConcertEntity concert, CancellationToken ct = default)
    {
        if (concert.BeginCancellation().TryGetError(out var beginError))
            throw new InvalidOperationException($"Concert cannot begin cancellation from {beginError.Current}.");
        if (concert.Cancel().TryGetError(out var cancelError))
            throw new InvalidOperationException($"Concert cannot cancel from {cancelError.Current}.");
        return Task.CompletedTask;
    }
}
