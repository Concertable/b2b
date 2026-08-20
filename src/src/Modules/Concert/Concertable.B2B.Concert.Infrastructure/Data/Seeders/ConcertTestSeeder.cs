using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Deal.Contracts;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

internal sealed class ConcertTestSeeder : ITestSeeder
{
    public int Order => 4;

    private readonly ConcertDbContext context;
    private readonly SeedState seed;
    private readonly IDealModule deals;
    private readonly ITermsFingerprintCalculator fingerprint;
    private readonly TimeProvider timeProvider;

    public ConcertTestSeeder(
        ConcertDbContext context,
        SeedState seed,
        IDealModule deals,
        ITermsFingerprintCalculator fingerprint,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.seed = seed;
        this.deals = deals;
        this.fingerprint = fingerprint;
        this.timeProvider = timeProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Opportunities.SeedIfEmptyAsync(async () =>
        {
            context.Opportunities.AddRange(seed.Opportunities);
            await context.SaveChangesAsync(ct);
        });

        await context.Applications.SeedIfEmptyAsync(async () =>
        {
            await SeededApplicationSigner.SignAsync(
                seed, deals, fingerprint, timeProvider.GetUtcNow().UtcDateTime, ct);
            context.Applications.AddRange(seed.Applications);
            await context.SaveChangesAsync(ct);

            context.Concerts.AddRange(seed.Concerts);
            await context.SaveChangesAsync(ct);
        });
    }
}
