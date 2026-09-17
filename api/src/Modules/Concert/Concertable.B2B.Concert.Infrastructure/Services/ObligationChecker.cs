using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ObligationChecker : IObligationChecker
{
    private static readonly ConcertState[] SettledStates =
    [
        ConcertState.Draft,
        ConcertState.Complete,
        ConcertState.Cancelled,
    ];

    private readonly IConcertReadDbContext context;
    private readonly TimeProvider timeProvider;

    public ObligationChecker(IConcertReadDbContext context, TimeProvider timeProvider)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task<bool> HasLiveAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return false;

        if (await context.Concerts
            .Where(c => tenantIds.Contains(c.VenueTenantId) || tenantIds.Contains(c.ArtistTenantId))
            .AnyAsync(c => !SettledStates.Contains(c.State), ct))
            return true;

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        return await context.SelfBillingAgreements
            .AnyAsync(s => tenantIds.Contains(s.TenantId) && s.ExpiresAtUtc > nowUtc, ct);
    }
}
