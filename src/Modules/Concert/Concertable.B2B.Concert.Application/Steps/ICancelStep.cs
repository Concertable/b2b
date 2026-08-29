using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Steps;

internal interface ICancelStep : IDealStrategy
{
    Task ExecuteAsync(ConcertEntity concert, CancellationToken ct = default);
}