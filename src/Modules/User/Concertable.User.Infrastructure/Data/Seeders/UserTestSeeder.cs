using Concertable.Application.Interfaces.Geometry;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Seeding.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.User.Infrastructure.Data.Seeders;

internal class UserTestSeeder : ITestSeeder
{
    public int Order => 0;

    private readonly UserDbContext context;
    private readonly SeedData seedData;
    private readonly IGeometryProvider geometryProvider;

    public UserTestSeeder(
        UserDbContext context,
        SeedData seedData,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider)
    {
        this.context = context;
        this.seedData = seedData;
        this.geometryProvider = geometryProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Users.SeedIfEmptyAsync(async () =>
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(SeedData.TestPassword);

            seedData.Customer = UserFactory.Customer("customer1@test.com", hash);
            seedData.VenueManager1 = UserFactory.VenueManager("venuemanager1@test.com", hash);
            seedData.VenueManager2 = UserFactory.VenueManager("venuemanager2@test.com", hash);
            seedData.ArtistManager1 = UserFactory.ArtistManager("artistmanager1@test.com", hash);

            seedData.ArtistManagerNoArtist = UserFactory.ArtistManager("artistmanager2@test.com", hash);
            seedData.ArtistManagerNoArtist.UpdateLocation(geometryProvider.CreatePoint(51, 0), new Address("Test County", "Test Town"));
            seedData.ArtistManagerNoArtist.UpdateAvatar("avatar.jpg");

            seedData.Admin = UserFactory.Admin("admin@test.com", hash);
            seedData.Admin.UpdateLocation(geometryProvider.CreatePoint(51, 0));
            seedData.Admin.UpdateAvatar("avatar.jpg");

            context.Users.AddRange(
                seedData.Customer,
                seedData.VenueManager1,
                seedData.VenueManager2,
                seedData.ArtistManager1,
                seedData.ArtistManagerNoArtist,
                seedData.Admin);

            context.VenueManagerProfiles.AddRange(
                new VenueManagerProfileEntity(seedData.VenueManager1.Id),
                new VenueManagerProfileEntity(seedData.VenueManager2.Id));

            context.ArtistManagerProfiles.AddRange(
                new ArtistManagerProfileEntity(seedData.ArtistManager1.Id),
                new ArtistManagerProfileEntity(seedData.ArtistManagerNoArtist.Id));

            context.AdminProfiles.Add(new AdminProfileEntity(seedData.Admin.Id));

            await context.SaveChangesAsync(ct);
        });
    }
}
