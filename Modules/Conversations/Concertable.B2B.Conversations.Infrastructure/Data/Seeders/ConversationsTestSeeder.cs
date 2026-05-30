using Concertable.DataAccess;
using Concertable.B2B.Conversations.Contracts;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Seeders;

internal class ConversationsTestSeeder : ITestSeeder
{
    public int Order => 6;

    private readonly ConversationsDbContext context;
    private readonly SeedState seedData;
    private readonly TimeProvider timeProvider;

    public ConversationsTestSeeder(ConversationsDbContext context, SeedState seedData, TimeProvider timeProvider)
    {
        this.context = context;
        this.seedData = seedData;
        this.timeProvider = timeProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Messages.SeedIfEmptyAsync(async () =>
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            context.Messages.AddRange(
                MessageEntity.Create(
                    seedData.ArtistManager1.Id,
                    seedData.VenueManager1.Id,
                    "Test inbox message — artist to venue.",
                    now.AddDays(-1),
                    MessageAction.ApplicationReceived),
                MessageEntity.Create(
                    seedData.VenueManager1.Id,
                    seedData.ArtistManager1.Id,
                    "Test inbox message — venue to artist.",
                    now,
                    MessageAction.ApplicationAccepted));

            await context.SaveChangesAsync(ct);
        });
    }
}
