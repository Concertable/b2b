using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Identity;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Seeders;

internal sealed class ConversationsTestSeeder : ITestSeeder
{
    public int Order => 6;

    private readonly ConversationsPrivilegedDbContext context;
    private readonly ConversationsDbContext migrations;
    private readonly SeedState seedData;
    private readonly TimeProvider timeProvider;

    public ConversationsTestSeeder(
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
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var venueUserId = seedData.VenueManager1.Id;
        var artistUserId = seedData.ArtistManager1.Id;
        var venueTenantId = TenantSeedIds.For(venueUserId);
        var artistTenantId = TenantSeedIds.For(artistUserId);

        await context.Messages.SeedIfEmptyAsync(async () =>
        {
            context.Messages.AddRange(
            [
                MessageEntity.Create(venueTenantId, artistTenantId, artistTenantId, artistUserId,
                    "Test inbox message — artist to venue.", now.AddDays(-1), MessageAction.ApplicationReceived),
                MessageEntity.Create(venueTenantId, artistTenantId, venueTenantId, venueUserId,
                    "Test inbox message — venue to artist.", now, MessageAction.ApplicationAccepted),
            ]);
            await context.SaveChangesAsync(ct);
        });
    }
}
