namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IObligationChecker
{
    Task<bool> HasLiveAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
