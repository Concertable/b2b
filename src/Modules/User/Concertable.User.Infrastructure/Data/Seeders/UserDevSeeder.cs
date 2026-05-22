using Concertable.Application.Interfaces.Geometry;
using Concertable.Shared.Infrastructure.Services.Geometry;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Seeding.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.User.Infrastructure.Data.Seeders;

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
            var hash = BCrypt.Net.BCrypt.HashPassword(SeedData.TestPassword);

            seedData.Admin = UserFactory.Admin("admin@test.com", hash);
            seedData.Admin.UpdateLocation(geometryProvider.CreatePoint(51.0, -0.5), new Address("Leicestershire", "Loughborough"));
            seedData.Admin.UpdateAvatar("avatar.jpg");
            context.Users.Add(seedData.Admin);

            seedData.Customer = UserFactory.Customer("customer1@test.com", hash);
            context.Users.Add(seedData.Customer);
            context.Users.Add(UserFactory.Customer("customer2@test.com", hash));
            context.Users.Add(UserFactory.Customer("customer3@test.com", hash));

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

        var customerEmails = new List<string> { "customer1@test.com", "customer2@test.com", "customer3@test.com" };
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
