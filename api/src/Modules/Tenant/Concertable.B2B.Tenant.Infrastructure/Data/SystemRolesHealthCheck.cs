using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class SystemRolesHealthCheck : IHealthCheck
{
    private readonly TenantDbContext context;

    public SystemRolesHealthCheck(TenantDbContext context)
    {
        this.context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthContext,
        CancellationToken cancellationToken = default)
    {
        var seeded = await context.Set<SystemRolesFingerprint>()
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return seeded?.Value == SystemRoles.Fingerprint
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The database's system roles were seeded from different definitions.");
    }
}
