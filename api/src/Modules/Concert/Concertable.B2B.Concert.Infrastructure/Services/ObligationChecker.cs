using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ObligationChecker : IObligationChecker
{
    // Concert states carrying no in-flight settlement — erasing a subject while a concert sits in any of these
    // breaks no settlement. Every other state is a blocking obligation, so a future lifecycle state defaults to
    // "blocking" until it is deliberately classified here.
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

    public async Task<int> CountLiveAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return 0;

        var unsettledConcerts = await context.Concerts
            .Where(c => tenantIds.Contains(c.VenueTenantId) || tenantIds.Contains(c.ArtistTenantId))
            .CountAsync(c => !SettledStates.Contains(c.State), ct);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var currentAgreements = await context.SelfBillingAgreements
            .CountAsync(s => tenantIds.Contains(s.TenantId) && s.ExpiresAtUtc > nowUtc, ct);

        return unsettledConcerts + currentAgreements;
    }
}
