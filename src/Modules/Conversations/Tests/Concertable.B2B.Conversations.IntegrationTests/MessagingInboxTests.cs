using Concertable.Seed.Identity;
using Xunit.Abstractions;

namespace Concertable.B2B.Conversations.IntegrationTests;

[Collection("Integration")]
public sealed class MessagingInboxTests : IAsyncLifetime
{
    private readonly ConversationsApiFixture fixture;

    public MessagingInboxTests(ConversationsApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync()
    {
        fixture.DetachOutput();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Inbox_ShowsTheMessagesTheVenueTenantReceived_NotItsOwnOutbound()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var page = await GetInboxAsync(venue);

        var message = Assert.Single(page.Data);
        Assert.Equal("Test inbox message — artist to venue.", message.Content);
    }

    [Fact]
    public async Task Inbox_EachPartySeesOnlyItsOwnReceivedSideOfTheThread()
    {
        var artist = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var page = await GetInboxAsync(artist);

        var message = Assert.Single(page.Data);
        Assert.Equal("Test inbox message — venue to artist.", message.Content);
    }

    [Fact]
    public async Task Inbox_ATenantNotPartyToTheThreadSeesNothing()
    {
        var otherVenue = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var page = await GetInboxAsync(otherVenue);

        Assert.Empty(page.Data);
    }

    [Fact]
    public async Task UnreadCount_ReflectsThePointer_AndDropsToZeroAfterMarkingTheThreadRead()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var before = await GetUnreadCountAsync(venue);
        Assert.Equal(1, before);

        var counterpartTenantId = TenantSeedIds.For(fixture.SeedState.ArtistManager1.Id);
        var markResponse = await venue.PostAsync("/api/Message/mark-read", new { CounterpartTenantId = counterpartTenantId });
        Assert.Equal(0, await markResponse.Content.ReadAsync<int>());

        Assert.Equal(0, await GetUnreadCountAsync(venue));
    }

    private static async Task<InboxPage> GetInboxAsync(HttpClient client) =>
        (await (await client.GetAsync("/api/Message/user")).Content.ReadAsync<InboxPage>())!;

    private static async Task<int> GetUnreadCountAsync(HttpClient client) =>
        await (await client.GetAsync("/api/Message/user/unread-count")).Content.ReadAsync<int>();

    private sealed record InboxPage(List<InboxMessage> Data, int TotalCount);
    private sealed record InboxMessage(int Id, InboxSender FromUser, string Content);
    private sealed record InboxSender(Guid Id, string Email);
}
