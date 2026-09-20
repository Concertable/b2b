using Concertable.B2B.Admin.Infrastructure.Data;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.Deal.Infrastructure.Data;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.User.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.Messaging.Infrastructure.Inbox;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

internal static class B2BMigrationJob
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var contextFactories = new Func<DbContext>[]
        {
            () => new UserDbContextFactory().CreateDbContext(connectionString),
            () => new TenantDbContextFactory().CreateDbContext(connectionString),
            () => new AdminDbContextFactory().CreateDbContext(connectionString),
            () => new ArtistDbContextFactory().CreateDbContext(connectionString),
            () => new VenueDbContextFactory().CreateDbContext(connectionString),
            () => new DealDbContextFactory().CreateDbContext(connectionString),
            () => new OpportunityDbContextFactory().CreateDbContext(connectionString),
            () => new ApplicationDbContextFactory().CreateDbContext(connectionString),
            () => new BookingDbContextFactory().CreateDbContext(connectionString),
            () => new ConcertDbContextFactory().CreateDbContext(connectionString),
            () => new ConversationsDbContextFactory().CreateDbContext(connectionString),
            () => new InboxDbContext(new DbContextOptionsBuilder<InboxDbContext>()
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Inbox", "messaging"))
                .Options),
            () => new OutboxDbContext(
                new DbContextOptionsBuilder<OutboxDbContext>()
                    .UseNpgsql(
                        connectionString,
                        npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Outbox", "messaging"))
                    .Options,
                Options.Create(new OutboxOptions())),
        };

        foreach (var createContext in contextFactories)
        {
            await using var context = createContext();
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
                throw new InvalidOperationException($"Migrations remain pending for {context.GetType().Name}.");
        }
    }
}
