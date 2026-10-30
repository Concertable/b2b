using Concertable.B2B.Authorization.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class TenantAuthorizationCatalogHealthCheck(TenantDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthContext,
        CancellationToken cancellationToken = default)
    {
        var installed = await context.Set<AuthorizationCatalogState>()
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return installed?.Revision == AuthorizationCatalog.Revision
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Installed authorization catalog revision differs from the server.");
    }
}
