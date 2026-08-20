using System.Net;
using Xunit.Abstractions;

namespace Concertable.B2B.Conversations.IntegrationTests;

[Collection("Integration")]
public sealed class ModerationApiTests : IAsyncLifetime
{
    private const string InboundMessage = "Test inbox message — artist to venue.";

    private readonly ConversationsApiFixture fixture;

    public ModerationApiTests(ConversationsApiFixture fixture, ITestOutputHelper output)
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
    public async Task Hide_RemovesTheMessageFromBothParticipants_AndRestorePutsItBack()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var artist = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var admin = fixture.CreateClient(fixture.SeedState.Admin);
        var messageId = await InboundMessageIdAsync(venue);
        var unreadBefore = await UnreadCountAsync(venue);

        await (await admin.PostAsync($"/api/Moderation/messages/{messageId}/hide")).ShouldBe(HttpStatusCode.NoContent);

        Assert.DoesNotContain((await GetInboxAsync(venue)).Data, m => m.Id == messageId);
        Assert.DoesNotContain((await GetInboxAsync(artist)).Data, m => m.Id == messageId);
        Assert.Equal(unreadBefore - 1, await UnreadCountAsync(venue));

        await (await admin.PostAsync($"/api/Moderation/messages/{messageId}/restore")).ShouldBe(HttpStatusCode.NoContent);

        Assert.Contains((await GetInboxAsync(venue)).Data, m => m.Id == messageId);
        Assert.Contains((await GetInboxAsync(artist)).Data, m => m.Id == messageId);
    }

    // The wrong-axis guard: tenant RBAC is scoped to one tenant, so a venue Owner must never be able to
    // moderate. Moderation is gated on the platform admin axis only.
    [Fact]
    public async Task Moderation_ShouldReturn403_ForATenantOwner()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var messageId = await InboundMessageIdAsync(venue);
        var reportId = await SubmitReportAsync(venue, messageId);

        await (await venue.GetAsync("/api/Moderation/reports")).ShouldBe(HttpStatusCode.Forbidden);
        await (await venue.PostAsync($"/api/Moderation/messages/{messageId}/hide")).ShouldBe(HttpStatusCode.Forbidden);
        await (await venue.PostAsync($"/api/Moderation/messages/{messageId}/restore")).ShouldBe(HttpStatusCode.Forbidden);
        await (await venue.PostAsync($"/api/Moderation/reports/{reportId}/resolve",
            new { outcome = "noActionTaken", notes = (string?)null })).ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Moderation_ShouldReturn401_WhenAnonymous()
    {
        var anonymous = fixture.CreateClient();

        await (await anonymous.GetAsync("/api/Moderation/reports")).ShouldBe(HttpStatusCode.Unauthorized);
        await (await anonymous.PostAsync("/api/Moderation/messages/1/hide")).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Resolve_RecordsTheOutcome_AndASecondResolveConflicts()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var admin = fixture.CreateClient(fixture.SeedState.Admin);
        var reportId = await SubmitReportAsync(venue, await InboundMessageIdAsync(venue));

        var resolve = await admin.PostAsync($"/api/Moderation/reports/{reportId}/resolve",
            new { outcome = "contentRemoved", notes = "message hidden" });
        await resolve.ShouldBe(HttpStatusCode.NoContent);

        var report = (await GetQueueAsync(admin)).Single(r => r.Id == reportId);
        Assert.Equal("contentRemoved", report.Outcome);
        Assert.Equal("message hidden", report.ResolutionNotes);
        Assert.NotNull(report.ResolvedAt);

        var second = await admin.PostAsync($"/api/Moderation/reports/{reportId}/resolve",
            new { outcome = "noActionTaken", notes = (string?)null });
        await second.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Queue_ReturnsReportsAcrossTenants_ForAnAdmin()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var admin = fixture.CreateClient(fixture.SeedState.Admin);
        var reportId = await SubmitReportAsync(venue, await InboundMessageIdAsync(venue));

        var queue = await GetQueueAsync(admin);

        var report = Assert.Single(queue, r => r.Id == reportId);
        Assert.Equal($"CR-{reportId}", report.Reference);
        Assert.Equal(InboundMessage, report.MessageExcerpt);
    }

    private async Task<int> SubmitReportAsync(HttpClient client, int messageId)
    {
        var response = await client.PostAsync($"/api/Message/{messageId}/report",
            new { category = "illegalContent", details = "unlawful" });
        await response.ShouldBe(HttpStatusCode.NoContent);

        var admin = fixture.CreateClient(fixture.SeedState.Admin);
        return (await GetQueueAsync(admin)).Single(r => r.MessageId == messageId).Id;
    }

    private static async Task<List<QueuedReport>> GetQueueAsync(HttpClient admin) =>
        (await (await admin.GetAsync("/api/Moderation/reports")).Content.ReadAsync<QueuePage>())!.Data;

    private static async Task<int> InboundMessageIdAsync(HttpClient client) =>
        (await GetInboxAsync(client)).Data.Single(m => m.Content == InboundMessage).Id;

    private static async Task<InboxPage> GetInboxAsync(HttpClient client) =>
        (await (await client.GetAsync("/api/Message/user")).Content.ReadAsync<InboxPage>())!;

    private static async Task<int> UnreadCountAsync(HttpClient client) =>
        await (await client.GetAsync("/api/Message/user/unread-count")).Content.ReadAsync<int>();

    private sealed record InboxPage(List<InboxMessage> Data);
    private sealed record QueuePage(List<QueuedReport> Data);
    private sealed record InboxMessage(int Id, string Content);
    private sealed record QueuedReport(
        int Id, string Reference, int MessageId, string MessageExcerpt,
        string? Outcome, DateTime? ResolvedAt, string? ResolutionNotes);
}
