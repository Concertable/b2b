using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Shared;
using Microsoft.EntityFrameworkCore;
using Concertable.B2B.Venue.Infrastructure.Data;

namespace Concertable.B2B.Venue.Infrastructure.Data.Seeders;

internal sealed class VenueDevSeeder : IDevSeeder
{
    public int Order => 2;

    private readonly VenueDbContext context;
    private readonly SeedState seed;
    private readonly ITenantScope tenantScope;

    public VenueDevSeeder(VenueDbContext context, SeedState seed, ITenantScope tenantScope)
    {
        this.context = context;
        this.seed = seed;
        this.tenantScope = tenantScope;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public Task SeedAsync(CancellationToken ct = default) =>
        context.SeedByTenantAsync(tenantScope, seed.Venues, ct);
}
