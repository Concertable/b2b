namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IObligationChecker
{
    Task<bool> HasLiveAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
