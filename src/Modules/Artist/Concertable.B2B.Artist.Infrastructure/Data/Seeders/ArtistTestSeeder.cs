using Concertable.Contracts;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.B2B.Seeding;
using Concertable.B2B.Seeding.Fakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Artist.Infrastructure.Data.Seeders;

internal class ArtistTestSeeder : ITestSeeder
{
    public int Order => 1;

    private readonly ArtistDbContext context;
    private readonly SeedData seed;
    private readonly IGeometryProvider geometryProvider;

    public ArtistTestSeeder(
        ArtistDbContext context,
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
        await context.Artists.SeedIfEmptyAsync(async () =>
        {
            seed.Artist = ArtistFaker.GetFaker(
                1,
                seed.ArtistManager1.Id,
                "Test Artist",
                "artist.jpg",
                "avatar.jpg",
                geometryProvider.CreatePoint(51, 0),
                new Address("Test County", "Test Town"),
                seed.ArtistManager1.Email,
                [Genre.Rock]).Generate();

            context.Artists.Add(seed.Artist);
            await context.SaveChangesAsync(ct);
        });
    }
}
