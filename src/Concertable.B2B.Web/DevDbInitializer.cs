using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Seeding;
using Concertable.Seeding.Fakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;

namespace Concertable.B2B.Web;

public class DevDbInitializer : IDbInitializer
{
    private readonly SeedData seedData;
    private readonly TimeProvider timeProvider;
    private readonly IGeometryProvider geometryProvider;
    private readonly ILocationFaker locationFaker;
    private readonly IEnumerable<IDevSeeder> seeders;
    private readonly OutboxDbContext outbox;
    private readonly InboxDbContext inbox;

    public DevDbInitializer(
        SeedData seedData,
        TimeProvider timeProvider,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        ILocationFaker locationFaker,
        IEnumerable<IDevSeeder> seeders,
        OutboxDbContext outbox,
        InboxDbContext inbox)
    {
        this.seedData = seedData;
        this.timeProvider = timeProvider;
        this.geometryProvider = geometryProvider;
        this.locationFaker = locationFaker;
        this.seeders = seeders;
        this.outbox = outbox;
        this.inbox = inbox;
    }

    public async Task InitializeAsync()
    {
        await outbox.Database.MigrateAsync();
        await inbox.Database.MigrateAsync();

        foreach (var seeder in seeders.OrderBy(s => s.Order))
            await seeder.MigrateAsync();

        foreach (var seeder in seeders.OrderBy(s => s.Order))
            await seeder.SeedAsync();
    }
}
