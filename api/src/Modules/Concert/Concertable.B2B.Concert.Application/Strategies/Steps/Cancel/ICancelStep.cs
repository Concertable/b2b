using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Strategies;

internal interface ICancelStep : IDealStep
{
    Task CancelAsync(ConcertEntity concert, CancellationToken ct = default);
}
