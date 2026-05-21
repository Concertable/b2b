using Concertable.DataAccess;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Customer.Infrastructure.Data.Seeders;

internal class CustomerTestSeeder : ITestSeeder
{
    public int Order => 7;

    private readonly CustomerDbContext context;
    private readonly SeedData seedData;

    public CustomerTestSeeder(CustomerDbContext context, SeedData seedData)
    {
        this.context = context;
        this.seedData = seedData;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Preferences.SeedIfEmptyAsync(async () =>
        {
            context.Preferences.Add(
                PreferenceEntity.Create(seedData.Customer.Id, 25, [Genre.Rock]));

            await context.SaveChangesAsync(ct);
        });
    }
}
