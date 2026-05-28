using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.B2B.Seeding;
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
        if (!await context.Users.AnyAsync(u => u.Id == SeedIds.Admin, ct))
        {
            seedData.Admin = UserEntity.FromRegistration(SeedIds.Admin, "admin@test.com", Role.Admin);
            seedData.Admin.UpdateLocation(geometryProvider.CreatePoint(51.0, -0.5), new Address("Leicestershire", "Loughborough"));
            seedData.Admin.UpdateAvatar("avatar.jpg");
            context.Users.Add(seedData.Admin);
        }

        var existingManagerIds = await context.Users
            .Where(u => u.Role == Role.ArtistManager || u.Role == Role.VenueManager)
            .Select(u => u.Id)
            .ToHashSetAsync(ct);

        seedData.ArtistManager1 = UserEntity.FromRegistration(SeedIds.ArtistManager(1), "artistmanager1@test.com", Role.ArtistManager);
        if (!existingManagerIds.Contains(SeedIds.ArtistManager(1)))
            context.Users.Add(seedData.ArtistManager1);

        if (!existingManagerIds.Contains(SeedIds.ArtistManager(2)))
            context.Users.Add(UserEntity.FromRegistration(SeedIds.ArtistManager(2), "artistmanager2@test.com", Role.ArtistManager));

        for (int i = 3; i <= 35; i++)
        {
            if (!existingManagerIds.Contains(SeedIds.ArtistManager(i)))
                context.Users.Add(UserEntity.FromRegistration(SeedIds.ArtistManager(i), $"artistmanager{i}@test.com", Role.ArtistManager));
        }

        seedData.VenueManager1 = UserEntity.FromRegistration(SeedIds.VenueManager(1), "venuemanager1@test.com", Role.VenueManager);
        if (!existingManagerIds.Contains(SeedIds.VenueManager(1)))
            context.Users.Add(seedData.VenueManager1);

        seedData.VenueManager2 = UserEntity.FromRegistration(SeedIds.VenueManager(2), "venuemanager2@test.com", Role.VenueManager);
        if (!existingManagerIds.Contains(SeedIds.VenueManager(2)))
            context.Users.Add(seedData.VenueManager2);

        for (int i = 3; i <= 35; i++)
        {
            if (!existingManagerIds.Contains(SeedIds.VenueManager(i)))
                context.Users.Add(UserEntity.FromRegistration(SeedIds.VenueManager(i), $"venuemanager{i}@test.com", Role.VenueManager));
        }

        if (context.ChangeTracker.HasChanges())
            await context.SaveChangesAsync(ct);

        var existingArtistProfileSubs = await context.ArtistManagerProfiles.Select(p => p.Sub).ToHashSetAsync(ct);
        var existingVenueProfileSubs = await context.VenueManagerProfiles.Select(p => p.Sub).ToHashSetAsync(ct);

        for (int i = 1; i <= 35; i++)
        {
            if (!existingArtistProfileSubs.Contains(SeedIds.ArtistManager(i)))
                context.ArtistManagerProfiles.Add(new ArtistManagerProfileEntity(SeedIds.ArtistManager(i)));
        }
        for (int i = 1; i <= 35; i++)
        {
            if (!existingVenueProfileSubs.Contains(SeedIds.VenueManager(i)))
                context.VenueManagerProfiles.Add(new VenueManagerProfileEntity(SeedIds.VenueManager(i)));
        }

        if (!await context.AdminProfiles.AnyAsync(p => p.Sub == SeedIds.Admin, ct))
            context.AdminProfiles.Add(new AdminProfileEntity(SeedIds.Admin));

        if (context.ChangeTracker.HasChanges())
            await context.SaveChangesAsync(ct);

        var usersByEmail = await context.Users.ToDictionaryAsync(u => u.Email, u => u.Id, ct);

        var artistManagerEmails = new List<string>();
        for (int i = 1; i <= 35; i++) artistManagerEmails.Add($"artistmanager{i}@test.com");
        seedData.ArtistManagerEmails = artistManagerEmails;
        seedData.ArtistManagerIds = [.. artistManagerEmails.Select(e => usersByEmail[e])];

        var venueManagerEmails = new List<string> { "venuemanager1@test.com", "venuemanager2@test.com" };
        for (int i = 3; i <= 35; i++) venueManagerEmails.Add($"venuemanager{i}@test.com");
        seedData.VenueManagerEmails = venueManagerEmails;
        seedData.VenueManagerIds = [.. venueManagerEmails.Select(e => usersByEmail[e])];

        seedData.Users = await context.Users.ToListAsync(ct);
    }
}
