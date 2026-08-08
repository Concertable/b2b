using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.Conversations.Infrastructure.Repositories;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.UnitTests.Repositories;

public sealed class MessageRepositoryTests
{
    private static readonly Guid VenueTenantId = Guid.NewGuid();
    private static readonly Guid ArtistTenantId = Guid.NewGuid();
    private static readonly Guid ArtistUserId = Guid.NewGuid();
    private static readonly Guid VenueMemberId = Guid.NewGuid();

    private static readonly DateTime Older = new(2026, 1, 1);
    private static readonly DateTime Between = new(2026, 1, 15);
    private static readonly DateTime Newer = new(2026, 2, 1);

    private static ConversationsDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<ConversationsDbContext>().UseInMemoryDatabase(dbName).Options,
            new ConversationsConfigurationProvider(),
            new StubTenantContext(VenueTenantId));

    private static MessageEntity FromArtist(DateTime sentDate) =>
        MessageEntity.Create(VenueTenantId, ArtistTenantId, ArtistTenantId, ArtistUserId, "received", sentDate);

    [Fact]
    public async Task GetUnreadCount_CountsOnlyMessagesNewerThanTheMembersReadPointer()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = NewContext(dbName))
        {
            seed.Messages.AddRange(FromArtist(Older), FromArtist(Newer));
            seed.ThreadReadStates.Add(ThreadReadStateEntity.Create(VenueTenantId, ArtistTenantId, VenueMemberId, Between));
            await seed.SaveChangesAsync();
        }

        await using var context = NewContext(dbName);
        var unread = await new MessageRepository(context).GetUnreadCountByTenantIdAsync(VenueTenantId, VenueMemberId);

        Assert.Equal(1, unread);
    }

    [Fact]
    public async Task GetUnreadCount_IsZeroWhenThePointerIsPastEveryReceivedMessage()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = NewContext(dbName))
        {
            seed.Messages.AddRange(FromArtist(Older), FromArtist(Newer));
            seed.ThreadReadStates.Add(ThreadReadStateEntity.Create(VenueTenantId, ArtistTenantId, VenueMemberId, Newer.AddDays(1)));
            await seed.SaveChangesAsync();
        }

        await using var context = NewContext(dbName);
        var unread = await new MessageRepository(context).GetUnreadCountByTenantIdAsync(VenueTenantId, VenueMemberId);

        Assert.Equal(0, unread);
    }

    private sealed class StubTenantContext : ITenantContext
    {
        public StubTenantContext(Guid tenantId) => TenantId = tenantId;

        public Guid? TenantId { get; }
        public bool IsHost => false;
    }
}
