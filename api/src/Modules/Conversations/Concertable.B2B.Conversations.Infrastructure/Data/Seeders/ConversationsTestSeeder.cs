using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Identity;
using Concertable.Seed.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Seeders;

internal sealed class ConversationsTestSeeder : ITestSeeder
{
    public int Order => 6;

    private readonly ConversationsDbContext context;
    private readonly SeedState seedData;
    private readonly ITenantScope tenantScope;
    private readonly TimeProvider timeProvider;

    public ConversationsTestSeeder(
        ConversationsDbContext context,
        SeedState seedData,
        ITenantScope tenantScope,
        TimeProvider timeProvider)
    {
        this.context = context;
        this.seedData = seedData;
        this.tenantScope = tenantScope;
        this.timeProvider = timeProvider;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public Task SeedAsync(CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var venueUserId = seedData.VenueManager1.Id;
        var artistUserId = seedData.ArtistManager1.Id;
        var venueTenantId = TenantSeedIds.For(venueUserId);
        var artistTenantId = TenantSeedIds.For(artistUserId);

        return context.SeedByVenueTenantAsync(
            tenantScope,
            [
                MessageEntity.Create(venueTenantId, artistTenantId, artistTenantId, artistUserId,
                    "Test inbox message — artist to venue.", now.AddDays(-1), MessageAction.ApplicationReceived),
                MessageEntity.Create(venueTenantId, artistTenantId, venueTenantId, venueUserId,
                    "Test inbox message — venue to artist.", now, MessageAction.ApplicationAccepted),
            ],
            ct);
    }
}
