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
    public async Task Inbox_ShowsTheWholeThread_WithInboundAttributedToTheCounterpartyOrg()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var page = await GetInboxAsync(venue);

        var inbound = page.Data.Single(m => m.Content == "Test inbox message — artist to venue.");
        Assert.Equal("org", inbound.Sender.Kind);
        Assert.Equal("The Rockers", inbound.Sender.DisplayName);
        Assert.Equal("Loughborough", inbound.Sender.Town);
    }

    [Fact]
    public async Task Inbox_AttributesTheTenantsOwnOutboundToTheMemberWhoSentIt()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var page = await GetInboxAsync(venue);

        var outbound = page.Data.Single(m => m.Content == "Test inbox message — venue to artist.");
        Assert.Equal("member", outbound.Sender.Kind);
        Assert.Equal(SeedUsers.VenueManagerEmail(1), outbound.Sender.DisplayName);
    }

    [Fact]
    public async Task Inbox_EachPartySeesTheCounterpartyOrgFromItsOwnSide()
    {
        var artist = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var page = await GetInboxAsync(artist);

        var inbound = page.Data.Single(m => m.Content == "Test inbox message — venue to artist.");
        Assert.Equal("org", inbound.Sender.Kind);
        Assert.Equal("The Grand Venue", inbound.Sender.DisplayName);
    }

    [Fact]
    public async Task Inbox_ATenantNotPartyToTheThreadSeesNothing()
    {
        var otherVenue = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var page = await GetInboxAsync(otherVenue);

        Assert.Empty(page.Data);
    }

    [Fact]
    public async Task UnreadCount_CountsInboundOnly_AndDropsToZeroAfterOpeningTheInbox()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var before = await GetUnreadCountAsync(venue);
        Assert.Equal(1, before);

        var markResponse = await venue.PostAsync("/api/Message/mark-read", new { });
        Assert.Equal(0, await markResponse.Content.ReadAsync<int>());

        Assert.Equal(0, await GetUnreadCountAsync(venue));
    }

    [Fact]
    public async Task Previews_ReturnLatestMessageWithCounterpartyIdentity()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await venue.GetAsync("/api/Message/previews");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var previews = await response.Content.ReadAsync<List<MessagePreview>>();
        var preview = Assert.Single(previews!);
        Assert.Equal("The Rockers", preview.OtherPartyName);
        Assert.Equal("Test inbox message — venue to artist.", preview.Preview);
        Assert.True(preview.Unread);
        Assert.Equal("/_venue/?inbox=open", preview.Href);
    }

    private static async Task<InboxPage> GetInboxAsync(HttpClient client) =>
        (await (await client.GetAsync("/api/Message/user")).Content.ReadAsync<InboxPage>())!;

    private static async Task<int> GetUnreadCountAsync(HttpClient client) =>
        await (await client.GetAsync("/api/Message/user/unread-count")).Content.ReadAsync<int>();

    private sealed record InboxPage(List<InboxMessage> Data, int TotalCount);
    private sealed record InboxMessage(int Id, InboxSender Sender, string Content);
    private sealed record InboxSender(string Kind, string DisplayName, string? County, string? Town);
    private sealed record MessagePreview(
        int Id,
        string OtherPartyName,
        string Preview,
        DateTime At,
        bool Unread,
        string Href);
}
