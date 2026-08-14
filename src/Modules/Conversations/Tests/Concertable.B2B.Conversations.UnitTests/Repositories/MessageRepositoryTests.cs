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

    private static MessageEntity FromArtist(Guid artistTenantId, DateTime sentDate, string content) =>
        MessageEntity.Create(VenueTenantId, artistTenantId, artistTenantId, ArtistUserId, content, sentDate);

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

    [Fact]
    public async Task GetRecentPreviews_ReturnsLatestMessageAndMemberUnreadStatePerCounterparty()
    {
        var dbName = Guid.NewGuid().ToString();
        var secondArtistTenantId = Guid.NewGuid();
        await using (var seed = NewContext(dbName))
        {
            seed.Messages.AddRange(
                FromArtist(ArtistTenantId, Older, "old first thread"),
                FromArtist(ArtistTenantId, Newer, "latest first thread"),
                FromArtist(secondArtistTenantId, Between, "second thread"),
                MessageEntity.Create(
                    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ArtistUserId,
                    "unrelated tenant thread", Newer.AddDays(1)));
            seed.ThreadReadStates.Add(ThreadReadStateEntity.Create(
                VenueTenantId, ArtistTenantId, VenueMemberId, Newer.AddMinutes(1)));
            await seed.SaveChangesAsync();
        }

        await using var context = NewContext(dbName);
        var previews = await new MessageRepository(context).GetRecentPreviewsAsync(VenueTenantId, VenueMemberId);

        Assert.Collection(
            previews,
            first =>
            {
                Assert.Equal("latest first thread", first.Preview);
                Assert.Equal(ArtistTenantId, first.CounterpartTenantId);
                Assert.False(first.CounterpartIsVenue);
                Assert.False(first.Unread);
            },
            second =>
            {
                Assert.Equal("second thread", second.Preview);
                Assert.Equal(secondArtistTenantId, second.CounterpartTenantId);
                Assert.True(second.Unread);
            });
    }

    private sealed class StubTenantContext : ITenantContext
    {
        public StubTenantContext(Guid tenantId) => TenantId = tenantId;

        public Guid? TenantId { get; }
        public bool IsHost => false;
    }
}
