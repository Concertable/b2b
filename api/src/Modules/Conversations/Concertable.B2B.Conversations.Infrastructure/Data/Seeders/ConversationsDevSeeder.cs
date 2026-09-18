using Concertable.B2B.Conversations.Contracts;
using Concertable.Seed.Identity;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Seeders;

internal sealed class ConversationsDevSeeder : IDevSeeder
{
    public int Order => 6;

    private readonly ConversationsDbContext context;
    private readonly SeedState seedData;
    private readonly TimeProvider timeProvider;

    public ConversationsDevSeeder(ConversationsDbContext context, SeedState seedData, TimeProvider timeProvider)
    {
        this.context = context;
        this.seedData = seedData;
        this.timeProvider = timeProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default) =>
        await context.Messages.SeedIfEmptyAsync(async () =>
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var artists = seedData.ArtistManagers;
            var venues = seedData.VenueManagers;

            if (artists.Count < 3 || venues.Count < 3)
                return;

            // Threads are saved first so the messages can reference real ids, exactly as sending does.
            var threads = new List<ThreadEntity>(3);
            for (var pair = 0; pair < 3; pair++)
            {
                threads.Add(ThreadEntity.Create(
                    [TenantSeedIds.For(venues[pair].Id), TenantSeedIds.For(artists[pair].Id)],
                    now.AddDays(-7)));
            }

            context.Threads.AddRange(threads);
            await context.SaveChangesAsync(ct);

            context.Messages.AddRange(
                FromArtist(threads[0].Id, artists[0].Id, "Hi — looking forward to the gig.", now.AddDays(-7)),
                FromVenue(threads[0].Id, venues[0].Id, "Your application has been accepted!", now.AddDays(-6), MessageAction.ApplicationAccepted),
                FromArtist(threads[1].Id, artists[1].Id, "Applied to your opportunity — thanks!", now.AddDays(-5), MessageAction.ApplicationReceived),
                FromArtist(threads[2].Id, artists[2].Id, "Setup needs an extra mic.", now.AddDays(-2)));

            await context.SaveChangesAsync(ct);
        });

    private static MessageEntity FromArtist(int threadId, Guid artistUserId, string content, DateTime sentDate, MessageAction? action = null) =>
        MessageEntity.Create(threadId, TenantSeedIds.For(artistUserId), artistUserId, content, sentDate, action);

    private static MessageEntity FromVenue(int threadId, Guid venueUserId, string content, DateTime sentDate, MessageAction? action = null) =>
        MessageEntity.Create(threadId, TenantSeedIds.For(venueUserId), venueUserId, content, sentDate, action);
}
