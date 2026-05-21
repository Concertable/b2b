using Concertable.Application.Interfaces.Geometry;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Seeding.Fakers;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Concertable.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Venue.Infrastructure.Data.Seeders;

internal class VenueTestSeeder : ITestSeeder
{
    public int Order => 2;

    private readonly VenueDbContext context;
    private readonly SeedData seed;
    private readonly IGeometryProvider geometryProvider;

    public VenueTestSeeder(
        VenueDbContext context,
        SeedData seed,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.context = context;
        this.seed = seed;
        this.geometryProvider = geometryProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Venues.SeedIfEmptyAsync(async () =>
        {
            seed.Venue = VenueFaker.GetFaker(
                seed.VenueManager1.Id,
                "Test Venue",
                "venue.jpg",
                "avatar.jpg",
                geometryProvider.CreatePoint(51, 0),
                new Address("Test County", "Test Town"),
                seed.VenueManager1.Email).Generate();

            context.Venues.Add(seed.Venue);
            await context.SaveChangesAsync(ct);
        });
    }
}
