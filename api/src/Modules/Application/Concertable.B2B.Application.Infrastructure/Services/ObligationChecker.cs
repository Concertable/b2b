using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ObligationChecker : IObligationChecker
{
    private readonly IApplicationReadDbContext context;

    public ObligationChecker(IApplicationReadDbContext context)
    {
        this.context = context;
    }

    public async Task<bool> HasLiveAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default)
    {
        if (tenantIds.Count == 0)
            return false;

        return await context.Applications
            .Where(a => tenantIds.Contains(a.VenueTenantId) || tenantIds.Contains(a.ArtistTenantId))
            .AnyAsync(a => !ApplicationObligation.SettledStates.Contains(a.State), ct);
    }
}
