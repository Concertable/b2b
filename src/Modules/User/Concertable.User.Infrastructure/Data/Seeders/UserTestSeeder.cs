using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;

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
            seedData.VenueManager1 = UserEntity.FromRegistration(SeedIds.VenueManager(1), "venuemanager1@test.com", Role.VenueManager);
            seedData.VenueManager2 = UserEntity.FromRegistration(SeedIds.VenueManager(2), "venuemanager2@test.com", Role.VenueManager);
            seedData.ArtistManager1 = UserEntity.FromRegistration(SeedIds.ArtistManager(1), "artistmanager1@test.com", Role.ArtistManager);

            seedData.ArtistManagerNoArtist = UserEntity.FromRegistration(SeedIds.ArtistManager(2), "artistmanager2@test.com", Role.ArtistManager);
            seedData.ArtistManagerNoArtist.UpdateLocation(geometryProvider.CreatePoint(51, 0), new Address("Test County", "Test Town"));
            seedData.ArtistManagerNoArtist.UpdateAvatar("avatar.jpg");

            seedData.Admin = UserEntity.FromRegistration(SeedIds.Admin, "admin@test.com", Role.Admin);
            seedData.Admin.UpdateLocation(geometryProvider.CreatePoint(51, 0));
            seedData.Admin.UpdateAvatar("avatar.jpg");

            context.Users.AddRange(
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
