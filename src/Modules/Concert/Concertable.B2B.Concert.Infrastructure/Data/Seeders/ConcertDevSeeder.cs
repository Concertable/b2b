using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

internal sealed class ConcertDevSeeder : IDevSeeder
{
    public int Order => 7;

    private readonly ConcertDbContext context;
    private readonly SeedState seed;
    private readonly ITenantModule tenants;
    private readonly LegalSettings legal;
    private readonly TimeProvider timeProvider;
    private readonly IBus bus;

    public ConcertDevSeeder(
        ConcertDbContext context,
        SeedState seed,
        ITenantModule tenants,
        IOptions<LegalSettings> legal,
        TimeProvider timeProvider,
        IBus bus)
    {
        this.context = context;
        this.seed = seed;
        this.tenants = tenants;
        this.legal = legal.Value;
        this.timeProvider = timeProvider;
        this.bus = bus;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Concerts.SeedIfEmptyAsync(async () =>
        {
            context.Concerts.AddRange(seed.Concerts);
            await context.SaveChangesAsync(ct);

            foreach (var concert in seed.Concerts)
                await bus.PublishAsync(new ConcertCreatedEvent(
                    concert.Id,
                    concert.ApplicationId,
                    concert.OpportunityId,
                    concert.ArtistId,
                    concert.VenueId,
                    concert.VenueTenantId,
                    concert.ArtistTenantId,
                    concert.Period.Start), ct);
        });

        await context.SelfBillingAgreements.SeedIfEmptyAsync(async () =>
        {
            await SeededSelfBillingAgreementGranter.GrantAsync(
                context, seed, tenants, legal.PlatformTermsVersion, timeProvider.GetUtcNow().UtcDateTime, ct);
            await context.SaveChangesAsync(ct);
        });
    }
}
