using Concertable.B2B.Conversations.Contracts;
using Concertable.Seed;
using Concertable.Seed.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Seeders;

internal class ConversationsDevSeeder : IDevSeeder
{
    public int Order => 6;

    private readonly ConversationsDbContext context;
    private readonly SeedData seedData;
    private readonly TimeProvider timeProvider;

    public ConversationsDevSeeder(ConversationsDbContext context, SeedData seedData, TimeProvider timeProvider)
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

            if (artists.Count == 0 || venues.Count == 0)
                return;

            context.Messages.AddRange(
                MessageEntity.Create(artists[0].Id, venues[0].Id, "Hi — looking forward to the gig.", now.AddDays(-7)),
                MessageEntity.Create(venues[0].Id, artists[0].Id, "Your application has been accepted!", now.AddDays(-6), MessageAction.ApplicationAccepted),
                MessageEntity.Create(artists[1].Id, venues[1].Id, "Applied to your opportunity — thanks!", now.AddDays(-5), MessageAction.ApplicationReceived),
                MessageEntity.Create(artists[2].Id, venues[2].Id, "Setup needs an extra mic.", now.AddDays(-2)));

            await context.SaveChangesAsync(ct);
        });
}
