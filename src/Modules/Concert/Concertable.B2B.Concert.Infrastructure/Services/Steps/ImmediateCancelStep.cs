using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ImmediateCancelStep : ICancelStep
{
    public Task ExecuteAsync(ConcertEntity concert, CancellationToken ct = default)
    {
        if (concert.BeginCancellation().TryGetError(out var beginError))
            throw new InvalidOperationException($"Concert cannot begin cancellation from {beginError.Current}.");
        if (concert.Cancel().TryGetError(out var cancelError))
            throw new InvalidOperationException($"Concert cannot cancel from {cancelError.Current}.");
        return Task.CompletedTask;
    }
}
