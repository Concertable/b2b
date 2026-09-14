using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ObligationChecker : IObligationChecker
{
    // Application states committing no money — an application still awaiting a decision, or one already refused or
    // withdrawn. Every other state is a blocking obligation, so a future lifecycle state defaults to "blocking"
    // until it is deliberately classified here.
    private static readonly ApplicationState[] SettledStates =
    [
        ApplicationState.Applied,
        ApplicationState.Rejected,
        ApplicationState.Withdrawn,
        ApplicationState.Cancelled,
    ];

    private readonly IApplicationReadDbContext context;

    public ObligationChecker(IApplicationReadDbContext context)
    {
        this.context = context;
    }

    public async Task<int> CountLiveAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return 0;

        return await context.Applications
            .Where(a => tenantIds.Contains(a.VenueTenantId) || tenantIds.Contains(a.ArtistTenantId))
            .CountAsync(a => !SettledStates.Contains(a.State), ct);
    }
}
