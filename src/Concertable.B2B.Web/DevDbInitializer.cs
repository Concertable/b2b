using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Seeding;
using Concertable.Seeding.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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
    private readonly ILogger<DevDbInitializer> logger;

    public DevDbInitializer(
        IEnumerable<IDevSeeder> seeders,
        OutboxDbContext outbox,
        InboxDbContext inbox,
        SeedingScope seedingScope,
        ILogger<DevDbInitializer> logger)
    {
        this.seeders = seeders;
        this.outbox = outbox;
        this.inbox = inbox;
        this.seedingScope = seedingScope;
        this.logger = logger;
    }

    public async Task InitializeAsync()
    {
        var ordered = seeders.OrderBy(s => s.Order).ToList();
        logger.BeginDbInitialization(ordered.Count);
        var total = Stopwatch.StartNew();

        await outbox.Database.MigrateAsync();
        await inbox.Database.MigrateAsync();

        foreach (var seeder in ordered)
        {
            logger.MigratingSeeder(seeder.GetType().Name);
            await seeder.MigrateAsync();
        }

        using (seedingScope.Activate())
        {
            foreach (var seeder in ordered)
            {
                var name = seeder.GetType().Name;
                logger.SeedingSeeder(name, seeder.Order);
                var sw = Stopwatch.StartNew();
                try
                {
                    await seeder.SeedAsync();
                    sw.Stop();
                    logger.SeederCompleted(name, sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    logger.SeederFailed(name, sw.ElapsedMilliseconds, ex);
                    throw;
                }
            }
        }

        total.Stop();
        logger.DbInitializationComplete(total.ElapsedMilliseconds);
    }
}
