using Concertable.Seed.Shared;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure.Data.Seeders;

internal class UserDevSeeder : IDevSeeder
{
    public int Order => 0;

    private readonly UserDbContext context;
    private readonly SeedState seedData;

    public UserDevSeeder(UserDbContext context, SeedState seedData)
    {
        this.context = context;
        this.seedData = seedData;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!await context.Users.AnyAsync(u => u.Id == seedData.Admin.Id, ct))
            context.Users.Add(seedData.Admin);

        if (!await context.AdminProfiles.AnyAsync(p => p.Sub == seedData.Admin.Id, ct))
            context.AdminProfiles.Add(new AdminProfileEntity(seedData.Admin.Id));

        if (context.ChangeTracker.HasChanges())
            await context.SaveChangesAsync(ct);
    }
}
