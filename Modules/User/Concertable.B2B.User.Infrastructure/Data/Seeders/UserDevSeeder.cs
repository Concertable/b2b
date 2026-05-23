using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;

namespace Concertable.B2B.User.Infrastructure.Data.Seeders;

internal static class SeedIds
{
    public static readonly Guid Admin = new("a0000000-0000-0000-0000-000000000001");

    public static Guid ArtistManager(int n) => new($"a1000000-0000-0000-0000-{n:D12}");
    public static Guid VenueManager(int n) => new($"b1000000-0000-0000-0000-{n:D12}");
}

internal class UserDevSeeder : IDevSeeder
{
    public int Order => 0;

    private readonly UserDbContext context;
    private readonly SeedData seedData;
    private readonly IGeometryProvider geometryProvider;

    public UserDevSeeder(
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
            seedData.Admin = UserEntity.FromRegistration(SeedIds.Admin, "admin@test.com", Role.Admin);
            seedData.Admin.UpdateLocation(geometryProvider.CreatePoint(51.0, -0.5), new Address("Leicestershire", "Loughborough"));
            seedData.Admin.UpdateAvatar("avatar.jpg");
            context.Users.Add(seedData.Admin);

            seedData.ArtistManager1 = UserEntity.FromRegistration(SeedIds.ArtistManager(1), "artistmanager1@test.com", Role.ArtistManager);
            context.Users.Add(seedData.ArtistManager1);

            context.Users.Add(UserEntity.FromRegistration(SeedIds.ArtistManager(2), "artistmanager2@test.com", Role.ArtistManager));

            for (int i = 3; i <= 35; i++)
                context.Users.Add(UserEntity.FromRegistration(SeedIds.ArtistManager(i), $"artistmanager{i}@test.com", Role.ArtistManager));

            seedData.VenueManager1 = UserEntity.FromRegistration(SeedIds.VenueManager(1), "venuemanager1@test.com", Role.VenueManager);
            context.Users.Add(seedData.VenueManager1);

            seedData.VenueManager2 = UserEntity.FromRegistration(SeedIds.VenueManager(2), "venuemanager2@test.com", Role.VenueManager);
            context.Users.Add(seedData.VenueManager2);

            for (int i = 3; i <= 35; i++)
                context.Users.Add(UserEntity.FromRegistration(SeedIds.VenueManager(i), $"venuemanager{i}@test.com", Role.VenueManager));

            await context.SaveChangesAsync(ct);

            var venueManagerIds = await context.Users
                .Where(u => u.Role == Role.VenueManager)
                .Select(u => u.Id)
                .ToListAsync(ct);
            context.VenueManagerProfiles.AddRange(venueManagerIds.Select(id => new VenueManagerProfileEntity(id)));

            var artistManagerIds = await context.Users
                .Where(u => u.Role == Role.ArtistManager)
                .Select(u => u.Id)
                .ToListAsync(ct);
            context.ArtistManagerProfiles.AddRange(artistManagerIds.Select(id => new ArtistManagerProfileEntity(id)));

            var adminIds = await context.Users
                .Where(u => u.Role == Role.Admin)
                .Select(u => u.Id)
                .ToListAsync(ct);
            context.AdminProfiles.AddRange(adminIds.Select(id => new AdminProfileEntity(id)));

            await context.SaveChangesAsync(ct);
        });

        var usersByEmail = await context.Users.ToDictionaryAsync(u => u.Email, u => u.Id, ct);

        var artistManagerEmails = new List<string>();
        for (int i = 1; i <= 35; i++) artistManagerEmails.Add($"artistmanager{i}@test.com");
        seedData.ArtistManagerEmails = artistManagerEmails;
        seedData.ArtistManagerIds = [.. artistManagerEmails.Select(e => usersByEmail[e])];

        var venueManagerEmails = new List<string> { "venuemanager1@test.com", "venuemanager2@test.com" };
        for (int i = 3; i <= 35; i++) venueManagerEmails.Add($"venuemanager{i}@test.com");
        seedData.VenueManagerEmails = venueManagerEmails;
        seedData.VenueManagerIds = [.. venueManagerEmails.Select(e => usersByEmail[e])];
    }
}
