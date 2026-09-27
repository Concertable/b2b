using Concertable.B2B.DataAccess.Application;
using Concertable.DataAccess;
using Concertable.Seed.Identity;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
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
        await SeedDisplaysAsync(ct);
        await context.Messages.SeedIfEmptyAsync(async () =>
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var venueUserId = seedData.VenueManager1.Id;
            var artistUserId = seedData.ArtistManager1.Id;
            var venueTenantId = TenantSeedIds.For(venueUserId);
            var artistTenantId = TenantSeedIds.For(artistUserId);
            var venueMembershipId = MembershipId(venueTenantId, venueUserId);
            var artistMembershipId = MembershipId(artistTenantId, artistUserId);
            var conversation = ConversationEntity.Create(
                [venueTenantId, artistTenantId], artistTenantId, artistUserId, now.AddDays(-1));
            context.Conversations.Add(conversation);
            await context.SaveChangesAsync(ct);

            var firstContent = "Test inbox message — artist to venue.";
            var first = MessageEntity.Create(
                conversation.Id,
                conversation.AllocateMessageSequence(),
                Guid.NewGuid(),
                ResourceCommandReceipt.HashPayload(firstContent, MessageAction.ApplicationReceived),
                artistTenantId,
                artistMembershipId,
                artistUserId,
                firstContent,
                now.AddDays(-1),
                MessageAction.ApplicationReceived);
            var secondContent = "Test inbox message — venue to artist.";
            var second = MessageEntity.Create(
                conversation.Id,
                conversation.AllocateMessageSequence(),
                Guid.NewGuid(),
                ResourceCommandReceipt.HashPayload(secondContent, MessageAction.ApplicationAccepted),
                venueTenantId,
                venueMembershipId,
                venueUserId,
                secondContent,
                now,
                MessageAction.ApplicationAccepted);
            context.Messages.AddRange(first, second);
            await context.SaveChangesAsync(ct);
        });
    }

    private Guid MembershipId(Guid tenantId, Guid userId) =>
        seedData.Memberships.Single(membership =>
            membership.TenantId == tenantId && membership.UserId == userId).Id;

    private async Task SeedDisplaysAsync(CancellationToken ct)
    {
        if (await context.TenantDisplays.AnyAsync(ct))
            return;
        context.TenantDisplays.AddRange(seedData.Tenants.Select(tenant =>
            TenantDisplay.Create(tenant.Id, tenant.DisplayVersion, tenant.EffectiveDisplayName)));
        await context.SaveChangesAsync(ct);
    }
}
