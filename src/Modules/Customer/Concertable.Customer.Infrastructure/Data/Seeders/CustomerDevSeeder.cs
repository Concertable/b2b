using Concertable.DataAccess;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Customer.Infrastructure.Data.Seeders;

internal class CustomerDevSeeder : IDevSeeder
{
    public int Order => 7;

    private readonly CustomerDbContext context;
    private readonly SeedData seedData;

    public CustomerDevSeeder(CustomerDbContext context, SeedData seedData)
    {
        this.context = context;
        this.seedData = seedData;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Preferences.SeedIfEmptyAsync(async () =>
        {
            var customerIds = seedData.CustomerIds;
            if (customerIds.Count < 3)
                return;

            context.Preferences.AddRange(
                PreferenceEntity.Create(customerIds[0], 10, [Genre.Rock]),
                PreferenceEntity.Create(customerIds[1], 25, []),
                PreferenceEntity.Create(customerIds[2], 50, []));

            await context.SaveChangesAsync(ct);
        });
    }
}
