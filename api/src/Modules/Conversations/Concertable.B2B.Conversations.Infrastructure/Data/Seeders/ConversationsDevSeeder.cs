using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Identity;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Seeders;

internal sealed class ConversationsDevSeeder : IDevSeeder
{
    public int Order => 6;

    private readonly ConversationsPrivilegedDbContext context;
    private readonly ConversationsDbContext migrations;
    private readonly SeedState seedData;
    private readonly TimeProvider timeProvider;

    public ConversationsDevSeeder(
        ConversationsPrivilegedDbContext context,
        ConversationsDbContext migrations,
        SeedState seedData,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.migrations = migrations;
        this.seedData = seedData;
        this.timeProvider = timeProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => migrations.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var artists = seedData.ArtistManagers;
        var venues = seedData.VenueManagers;

        if (artists.Count < 3 || venues.Count < 3)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        await context.Messages.SeedIfEmptyAsync(async () =>
        {
            context.Messages.AddRange(
            [
                FromArtist(venues[0].Id, artists[0].Id, "Hi — looking forward to the gig.", now.AddDays(-7)),
                FromVenue(venues[0].Id, artists[0].Id, "Your application has been accepted!", now.AddDays(-6), MessageAction.ApplicationAccepted),
                FromArtist(venues[1].Id, artists[1].Id, "Applied to your opportunity — thanks!", now.AddDays(-5), MessageAction.ApplicationReceived),
                FromArtist(venues[2].Id, artists[2].Id, "Setup needs an extra mic.", now.AddDays(-2)),
            ]);
            await context.SaveChangesAsync(ct);
        });
    }

    private static MessageEntity FromArtist(Guid venueUserId, Guid artistUserId, string content, DateTime sentDate, MessageAction? action = null) =>
        MessageEntity.Create(TenantSeedIds.For(venueUserId), TenantSeedIds.For(artistUserId), TenantSeedIds.For(artistUserId), artistUserId, content, sentDate, action);

    private static MessageEntity FromVenue(Guid venueUserId, Guid artistUserId, string content, DateTime sentDate, MessageAction? action = null) =>
        MessageEntity.Create(TenantSeedIds.For(venueUserId), TenantSeedIds.For(artistUserId), TenantSeedIds.For(venueUserId), venueUserId, content, sentDate, action);
}
