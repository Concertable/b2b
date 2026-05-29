using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.B2B.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure.Data.Seeders;

internal class UserTestSeeder : ITestSeeder
{
    public int Order => 0;

    private readonly UserDbContext context;
    private readonly SeedData seedData;

    public UserTestSeeder(UserDbContext context, SeedData seedData)
    {
        this.context = context;
        this.seedData = seedData;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Users.SeedIfEmptyAsync(async () =>
        {
            context.Users.AddRange(seedData.Users);
            context.ArtistManagerProfiles.AddRange(
                seedData.ArtistManagers.Select(u => new ArtistManagerProfileEntity(u.Id)));
            context.VenueManagerProfiles.AddRange(
                seedData.VenueManagers.Select(u => new VenueManagerProfileEntity(u.Id)));
            context.AdminProfiles.Add(new AdminProfileEntity(seedData.Admin.Id));

            await context.SaveChangesAsync(ct);
        });
    }
}
