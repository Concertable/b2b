using Concertable.DataAccess;
using Concertable.B2B.Conversations.Contracts;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
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

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.Messages.SeedIfEmptyAsync(async () =>
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var customers = seedData.CustomerIds;
            var artists = seedData.ArtistManagerIds;
            var venues = seedData.VenueManagerIds;

            if (customers.Count == 0 || artists.Count == 0 || venues.Count == 0)
                return;

            context.Messages.AddRange(
                MessageEntity.Create(artists[0], venues[0], "Hi — looking forward to the gig.", now.AddDays(-7)),
                MessageEntity.Create(venues[0], artists[0], "Your application has been accepted!", now.AddDays(-6), MessageAction.ApplicationAccepted),
                MessageEntity.Create(artists[1], venues[1], "Applied to your opportunity — thanks!", now.AddDays(-5), MessageAction.ApplicationReceived),
                MessageEntity.Create(venues[2], customers[0], "Thanks for buying a ticket.", now.AddDays(-4)),
                MessageEntity.Create(artists[2], venues[2], "Setup needs an extra mic.", now.AddDays(-2)));

            await context.SaveChangesAsync(ct);
        });
    }
}
