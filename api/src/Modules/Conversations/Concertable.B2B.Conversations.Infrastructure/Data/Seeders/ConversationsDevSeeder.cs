using Concertable.B2B.DataAccess.Application;
using Concertable.Seed.Identity;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
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
        await SeedDisplaysAsync(ct);
        await context.Messages.SeedIfEmptyAsync(async () =>
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var artists = seedData.ArtistManagers;
            var venues = seedData.VenueManagers;
            if (artists.Count < 3 || venues.Count < 3)
                return;

            var conversations = new List<ConversationEntity>(3);
            for (var pair = 0; pair < 3; pair++)
            {
                var artistTenantId = TenantSeedIds.For(artists[pair].Id);
                conversations.Add(ConversationEntity.Create(
                    [TenantSeedIds.For(venues[pair].Id), artistTenantId],
                    artistTenantId,
                    artists[pair].Id,
                    now.AddDays(-7)));
            }
            context.Conversations.AddRange(conversations);
            await context.SaveChangesAsync(ct);

            context.Messages.AddRange(
                Message(conversations[0], artists[0].Id, "Hi — looking forward to the gig.", now.AddDays(-7)),
                Message(conversations[0], venues[0].Id, "Your application has been accepted!", now.AddDays(-6), MessageAction.ApplicationAccepted),
                Message(conversations[1], artists[1].Id, "Applied to your opportunity — thanks!", now.AddDays(-5), MessageAction.ApplicationReceived),
                Message(conversations[2], artists[2].Id, "Setup needs an extra mic.", now.AddDays(-2)));
            await context.SaveChangesAsync(ct);
        });
    }

    private MessageEntity Message(
        ConversationEntity conversation,
        Guid userId,
        string content,
        DateTime sentAt,
        MessageAction? action = null)
    {
        var tenantId = TenantSeedIds.For(userId);
        var membershipId = seedData.Memberships.Single(membership =>
            membership.TenantId == tenantId && membership.UserId == userId).Id;
        return MessageEntity.Create(
            conversation.Id,
            conversation.AllocateMessageSequence(),
            Guid.NewGuid(),
            ResourceCommandReceipt.HashPayload(content, action),
            tenantId,
            membershipId,
            userId,
            content,
            sentAt,
            action);
    }

    private async Task SeedDisplaysAsync(CancellationToken ct)
    {
        if (await context.TenantDisplays.AnyAsync(ct))
            return;
        context.TenantDisplays.AddRange(seedData.Tenants.Select(tenant =>
            TenantDisplay.Create(tenant.Id, tenant.DisplayVersion, tenant.EffectiveDisplayName)));
        await context.SaveChangesAsync(ct);
    }
}
