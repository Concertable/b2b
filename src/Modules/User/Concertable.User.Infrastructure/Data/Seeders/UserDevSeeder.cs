using Concertable.Application.Interfaces.Geometry;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Seeding.Factories;
using Concertable.Seeding.Fakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.User.Infrastructure.Data.Seeders;

internal class UserDevSeeder : IDevSeeder
{
    public int Order => 0;

    private readonly UserDbContext context;
    private readonly SeedData seedData;
    private readonly IGeometryProvider geometryProvider;
    private readonly ILocationFaker locationFaker;

    public UserDevSeeder(
        UserDbContext context,
        SeedData seedData,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        ILocationFaker locationFaker)
    {
        this.context = context;
        this.seedData = seedData;
        this.geometryProvider = geometryProvider;
        this.locationFaker = locationFaker;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Users.SeedIfEmptyAsync(async () =>
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(SeedData.TestPassword);

            seedData.Admin = UserFactory.Admin("admin@test.com", hash);
            seedData.Admin.UpdateLocation(geometryProvider.CreatePoint(51.0, -0.5), new Address("Leicestershire", "Loughborough"));
            seedData.Admin.UpdateAvatar("avatar.jpg");
            context.Users.Add(seedData.Admin);

            var customerLoc = locationFaker.Next();
            seedData.Customer = UserFactory.Customer("customer1@test.com", hash);
            seedData.Customer.UpdateLocation(geometryProvider.CreatePoint(customerLoc.Latitude, customerLoc.Longitude), new Address(customerLoc.County, customerLoc.Town));
            seedData.Customer.UpdateAvatar("avatar.jpg");
            context.Users.Add(seedData.Customer);

            for (int i = 2; i <= 6; i++)
            {
                var loc = locationFaker.Next();
                var c = UserFactory.Customer($"customer{i}@test.com", hash);
                c.UpdateLocation(geometryProvider.CreatePoint(loc.Latitude, loc.Longitude), new Address(loc.County, loc.Town));
                c.UpdateAvatar("avatar.jpg");
                context.Users.Add(c);
            }

            seedData.ArtistManager1 = UserFactory.ArtistManager("artistmanager1@test.com", hash);
            context.Users.Add(seedData.ArtistManager1);

            var artistManager2 = UserFactory.ArtistManager("artistmanager2@test.com", hash);
            context.Users.Add(artistManager2);

            for (int i = 3; i <= 35; i++)
                context.Users.Add(UserFactory.ArtistManager($"artistmanager{i}@test.com", hash));

            seedData.VenueManager1 = UserFactory.VenueManager("venuemanager1@test.com", hash);
            context.Users.Add(seedData.VenueManager1);

            seedData.VenueManager2 = UserFactory.VenueManager("venuemanager2@test.com", hash);
            context.Users.Add(seedData.VenueManager2);

            for (int i = 3; i <= 35; i++)
                context.Users.Add(UserFactory.VenueManager($"venuemanager{i}@test.com", hash));

            // EF assigns GUIDs when entities are tracked, so IDs are available before SaveChanges
            await context.SaveChangesAsync(ct);

            // Insert profiles for all B2B managers and admins
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

        // Always resolve IDs from DB so downstream seeders work on first run and restarts
        var usersByEmail = await context.Users.ToDictionaryAsync(u => u.Email, u => u.Id, ct);

        var customerEmails = new List<string> { "customer1@test.com" };
        for (int i = 2; i <= 6; i++) customerEmails.Add($"customer{i}@test.com");
        seedData.CustomerEmails = customerEmails;
        seedData.CustomerIds = [.. customerEmails.Select(e => usersByEmail[e])];

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
