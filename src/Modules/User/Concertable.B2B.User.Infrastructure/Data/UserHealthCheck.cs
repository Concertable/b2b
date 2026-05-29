using Concertable.Seeding.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Concertable.B2B.User.Infrastructure.Data;

internal sealed class UserHealthCheck : IHealthCheck
{
    private readonly UserDbContext context;

    public UserHealthCheck(UserDbContext context)
    {
        this.context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var count = await this.context.Users.CountAsync(cancellationToken);
        return count >= SeedUsers.TotalCount
            ? HealthCheckResult.Healthy($"users={count}")
            : HealthCheckResult.Unhealthy($"users={count}/{SeedUsers.TotalCount}");
    }
}
