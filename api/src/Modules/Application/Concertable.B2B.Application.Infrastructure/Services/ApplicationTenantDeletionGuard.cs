using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationTenantDeletionGuard(
    ApplicationPrivilegedDbContext context,
    CommandTransactionAccessor transactions) : ITenantDeletionGuard
{
    public async Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Tenant deletion requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        return await context.Applications.AnyAsync(application =>
            (application.VenueTenantId == tenantId || application.ArtistTenantId == tenantId)
            && (application.State == ApplicationState.Applied || application.State == ApplicationState.Accepted),
            ct);
    }
}
