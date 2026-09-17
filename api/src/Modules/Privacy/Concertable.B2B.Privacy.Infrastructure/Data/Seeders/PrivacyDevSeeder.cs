using Concertable.Seed.Shared;
using Concertable.B2B.Privacy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Privacy.Infrastructure.Data.Seeders;

/// <summary>Migrate-only: the Privacy module holds no seed data (subject-erasure requests are operational, never
/// seeded), but its context still needs migrating so the schema exists.</summary>
internal sealed class PrivacyDevSeeder : IDevSeeder
{
    public int Order => 1;

    private readonly PrivacyDbContext context;

    public PrivacyDevSeeder(PrivacyDbContext context)
    {
        this.context = context;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}
