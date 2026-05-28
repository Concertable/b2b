using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Seeding;
using Concertable.Seeding.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;

namespace Concertable.B2B.Web;

public class DevDbInitializer : IDbInitializer
{
    private readonly IEnumerable<IDevSeeder> seeders;
    private readonly OutboxDbContext outbox;
    private readonly InboxDbContext inbox;
    private readonly SeedingScope seedingScope;

    public DevDbInitializer(
        IEnumerable<IDevSeeder> seeders,
        OutboxDbContext outbox,
        InboxDbContext inbox,
        SeedingScope seedingScope)
    {
        this.seeders = seeders;
        this.outbox = outbox;
        this.inbox = inbox;
        this.seedingScope = seedingScope;
    }

    public async Task InitializeAsync()
    {
        await outbox.Database.MigrateAsync();
        await inbox.Database.MigrateAsync();

        foreach (var seeder in seeders.OrderBy(s => s.Order))
            await seeder.MigrateAsync();

        using (seedingScope.Activate())
        {
            foreach (var seeder in seeders.OrderBy(s => s.Order))
                await seeder.SeedAsync();
        }
    }
}
