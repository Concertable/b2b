using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Strategies;

internal interface ICancel : IDealStrategy
{
    Task CancelAsync(ConcertEntity concert, CancellationToken ct = default);
}
