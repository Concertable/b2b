using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.B2B.Admin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Admin.Infrastructure.Data.Seeders;

internal sealed class AdminTestSeeder : ITestSeeder
{
    public int Order => 1;

    private readonly AdminDbContext context;
    private readonly SeedState seedData;

    public AdminTestSeeder(AdminDbContext context, SeedState seedData)
    {
        this.context = context;
        this.seedData = seedData;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.AdminProfiles.SeedIfEmptyAsync(async () =>
        {
            context.AdminProfiles.Add(new AdminProfileEntity(seedData.Admin.Id));
            await context.SaveChangesAsync(ct);
        });
}
