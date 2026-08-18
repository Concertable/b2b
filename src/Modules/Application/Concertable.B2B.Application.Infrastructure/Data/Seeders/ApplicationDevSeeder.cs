using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Data.Seeders;

internal sealed class ApplicationDevSeeder : IDevSeeder
{
    public int Order => 5;

    private readonly ApplicationDbContext context;
    private readonly SeedState seed;
    private readonly IDealModule deals;
    private readonly ITermsFingerprintCalculator fingerprint;
    private readonly TimeProvider timeProvider;

    public ApplicationDevSeeder(
        ApplicationDbContext context,
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

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Applications.SeedIfEmptyAsync(async () =>
        {
            await SeededApplicationSigner.SignAsync(
                seed, deals, fingerprint, timeProvider.GetUtcNow().UtcDateTime, ct);
            context.Applications.AddRange(seed.Applications);
            await context.SaveChangesAsync(ct);
        });
}
