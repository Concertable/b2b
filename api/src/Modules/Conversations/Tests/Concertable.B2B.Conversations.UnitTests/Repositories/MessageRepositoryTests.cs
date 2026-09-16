using Concertable.Contracts;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.Conversations.Infrastructure.Repositories;
using Concertable.B2B.DataAccess.Infrastructure;
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

    /* The queries under test are named for the tenant they serve and say so in SQL, so the context runs
       host-stanced here: the ambient grant filter needs a real provider and belongs to the integration tier. */
    private static ConversationsDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<ConversationsDbContext>().UseInMemoryDatabase(dbName).Options,
            new ConversationsConfigurationProvider(),
            new StubTenantContext(VenueTenantId),
            DesignTimeAccessContext.Instance);

    private static async Task<ThreadEntity> AddThreadAsync(ConversationsDbContext context, Guid counterpartTenantId)
    {
        var thread = ThreadEntity.Create([VenueTenantId, counterpartTenantId], Older);
        context.Threads.Add(thread);
        await context.SaveChangesAsync();
        return thread;
    }

    private static MessageEntity FromArtist(int threadId, DateTime sentDate, string content = "received") =>
        MessageEntity.Create(threadId, ArtistTenantId, ArtistUserId, content, sentDate);

    [Fact]
    public async Task GetUnreadCountByTenantIdAsync_CountsOnlyMessagesNewerThanTheMembersReadPointer()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = NewContext(dbName))
        {
            var thread = await AddThreadAsync(seed, ArtistTenantId);
            seed.Messages.AddRange(FromArtist(thread.Id, Older), FromArtist(thread.Id, Newer));
            seed.ThreadReadStates.Add(
                ThreadReadStateEntity.Create(thread.Id, VenueTenantId, VenueMemberId, Between));
            await seed.SaveChangesAsync();
        }

        await using var context = NewContext(dbName);
        var unread = await new MessageRepository(context).GetUnreadCountByTenantIdAsync(VenueTenantId, VenueMemberId);

        Assert.Equal(1, unread);
    }

    [Fact]
    public async Task GetUnreadCountByTenantIdAsync_PointerPastEveryReceivedMessage_IsZero()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = NewContext(dbName))
        {
            var thread = await AddThreadAsync(seed, ArtistTenantId);
            seed.Messages.AddRange(FromArtist(thread.Id, Older), FromArtist(thread.Id, Newer));
            seed.ThreadReadStates.Add(
                ThreadReadStateEntity.Create(thread.Id, VenueTenantId, VenueMemberId, Newer.AddDays(1)));
            await seed.SaveChangesAsync();
        }

        await using var context = NewContext(dbName);
        var unread = await new MessageRepository(context).GetUnreadCountByTenantIdAsync(VenueTenantId, VenueMemberId);

        Assert.Equal(0, unread);
    }

    [Fact]
    public async Task GetRecentPreviewsAsync_ReturnsLatestMessageAndMemberUnreadStatePerThread()
    {
        var dbName = Guid.NewGuid().ToString();
        var secondArtistTenantId = Guid.NewGuid();
        await using (var seed = NewContext(dbName))
        {
            var first = await AddThreadAsync(seed, ArtistTenantId);
            var second = await AddThreadAsync(seed, secondArtistTenantId);

            var unrelatedTenantId = Guid.NewGuid();
            var unrelated = ThreadEntity.Create([unrelatedTenantId, Guid.NewGuid()], Older);
            seed.Threads.Add(unrelated);
            await seed.SaveChangesAsync();

            seed.Messages.AddRange(
                FromArtist(first.Id, Older, "old first thread"),
                FromArtist(first.Id, Newer, "latest first thread"),
                MessageEntity.Create(second.Id, secondArtistTenantId, ArtistUserId, "second thread", Between),
                MessageEntity.Create(unrelated.Id, unrelatedTenantId, ArtistUserId, "unrelated thread", Newer.AddDays(1)));
            seed.ThreadReadStates.Add(
                ThreadReadStateEntity.Create(first.Id, VenueTenantId, VenueMemberId, Newer.AddMinutes(1)));
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
                Assert.False(first.Unread);
            },
            second =>
            {
                Assert.Equal("second thread", second.Preview);
                Assert.Equal(secondArtistTenantId, second.CounterpartTenantId);
                Assert.True(second.Unread);
            });
    }

    [Fact]
    public async Task GetByTenantIdAsync_HiddenMessages_AreExcludedFromTheInboxAndTheUnreadCount()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = NewContext(dbName))
        {
            var thread = await AddThreadAsync(seed, ArtistTenantId);
            var visible = FromArtist(thread.Id, Older);
            var hidden = FromArtist(thread.Id, Newer);
            hidden.Hide(Guid.NewGuid(), Newer.AddDays(1));
            seed.Messages.AddRange(visible, hidden);
            await seed.SaveChangesAsync();
        }

        await using var context = NewContext(dbName);
        var repository = new MessageRepository(context);

        var page = await repository.GetByTenantIdAsync(VenueTenantId, new PageParams());

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(Older, page.Data.Single().SentDate);
        Assert.Equal(1, await repository.GetUnreadCountByTenantIdAsync(VenueTenantId, VenueMemberId));
    }

    [Fact]
    public async Task GetRecentPreviewsAsync_HiddenLatestMessage_ReturnsVisibleMessageAsRead()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = NewContext(dbName))
        {
            var thread = await AddThreadAsync(seed, ArtistTenantId);
            var visible = FromArtist(thread.Id, Older);
            var hidden = FromArtist(thread.Id, Newer);
            hidden.Hide(Guid.NewGuid(), Newer.AddDays(1));
            seed.Messages.AddRange(visible, hidden);
            seed.ThreadReadStates.Add(
                ThreadReadStateEntity.Create(thread.Id, VenueTenantId, VenueMemberId, Between));
            await seed.SaveChangesAsync();
        }

        await using var context = NewContext(dbName);
        var previews = await new MessageRepository(context).GetRecentPreviewsAsync(VenueTenantId, VenueMemberId);

        var preview = Assert.Single(previews);
        Assert.Equal(Older, preview.At);
        Assert.False(preview.Unread);
    }

    private sealed class StubTenantContext : ITenantContext
    {
        public StubTenantContext(Guid tenantId) => TenantId = tenantId;

        public Guid? TenantId { get; }

        public bool IsHost => false;
    }
}
