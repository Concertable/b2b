using Concertable.B2B.Seed.Infrastructure;
using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Seed.Shared;
using Concertable.B2B.Seed.Infrastructure.Fakers;
using Concertable.Seed.Shared.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Application;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public class TestDbInitializer : IDbInitializer
{
    private readonly IGeometryProvider geometryProvider;
    private readonly SeedState seed;
    private readonly ILocationFaker locationFaker;
    private readonly TimeProvider timeProvider;
    private readonly IEnumerable<ITestSeeder> seeders;
    private readonly InboxDbContext inboxDbContext;
    private readonly OutboxDbContext outboxDbContext;
    private readonly SeedingScope seedingScope;

    public TestDbInitializer(
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        SeedState seed,
        ILocationFaker locationFaker,
        TimeProvider timeProvider,
        IEnumerable<ITestSeeder> seeders,
        InboxDbContext inboxDbContext,
        OutboxDbContext outboxDbContext,
        SeedingScope seedingScope)
    {
        this.geometryProvider = geometryProvider;
        this.seed = seed;
        this.locationFaker = locationFaker;
        this.timeProvider = timeProvider;
        this.seeders = seeders;
        this.inboxDbContext = inboxDbContext;
        this.outboxDbContext = outboxDbContext;
        this.seedingScope = seedingScope;
    }

    public async Task InitializeAsync()
    {
        await inboxDbContext.Database.MigrateAsync();
        await outboxDbContext.Database.MigrateAsync();

        foreach (var seeder in seeders.OrderBy(s => s.Order))
            await seeder.MigrateAsync();

        using (seedingScope.Activate())
        {
            foreach (var seeder in seeders.OrderBy(s => s.Order))
                await seeder.SeedAsync();
        }
    }
}
