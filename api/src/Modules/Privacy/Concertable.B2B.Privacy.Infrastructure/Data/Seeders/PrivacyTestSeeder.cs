using Concertable.Seed.Shared;
using Concertable.B2B.Privacy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Privacy.Infrastructure.Data.Seeders;

internal sealed class PrivacyTestSeeder : ITestSeeder
{
    public int Order => 1;

    private readonly PrivacyDbContext context;

    public PrivacyTestSeeder(PrivacyDbContext context)
    {
        this.context = context;
    }

    public Task MigrateAsync(CancellationToken ct = default) => this.context.Database.MigrateAsync(ct);

    public Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}
