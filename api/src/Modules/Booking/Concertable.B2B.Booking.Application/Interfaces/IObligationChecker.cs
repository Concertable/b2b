namespace Concertable.B2B.Booking.Application.Interfaces;

internal interface IObligationChecker
{
    Task<bool> HasLiveAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
