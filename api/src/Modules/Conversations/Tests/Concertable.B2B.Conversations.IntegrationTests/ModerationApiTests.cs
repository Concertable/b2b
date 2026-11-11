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
        var inbound = await InboundMessageAsync(venue);
        var unreadBefore = await UnreadCountAsync(venue);

        await (await admin.PostAsync($"/api/Moderation/messages/{inbound.Id}/hide")).ShouldBe(HttpStatusCode.NoContent);

        Assert.DoesNotContain(await GetMessagesAsync(venue), message => message.Id == inbound.Id);
        Assert.DoesNotContain(await GetMessagesAsync(artist), message => message.Id == inbound.Id);
        Assert.Equal(unreadBefore - 1, await UnreadCountAsync(venue));

        await (await admin.PostAsync($"/api/Moderation/messages/{inbound.Id}/restore")).ShouldBe(HttpStatusCode.NoContent);

        Assert.Contains(await GetMessagesAsync(venue), message => message.Id == inbound.Id);
        Assert.Contains(await GetMessagesAsync(artist), message => message.Id == inbound.Id);
    }

    [Fact]
    public async Task Moderation_ShouldReturn403_ForATenantOwner()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var inbound = await InboundMessageAsync(venue);
        var reportId = await SubmitReportAsync(venue, inbound);

        await (await venue.GetAsync("/api/Moderation/reports")).ShouldBe(HttpStatusCode.Forbidden);
        await (await venue.PostAsync($"/api/Moderation/messages/{inbound.Id}/hide")).ShouldBe(HttpStatusCode.Forbidden);
        await (await venue.PostAsync($"/api/Moderation/messages/{inbound.Id}/restore")).ShouldBe(HttpStatusCode.Forbidden);
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
        var reportId = await SubmitReportAsync(venue, await InboundMessageAsync(venue));

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
        var reportId = await SubmitReportAsync(venue, await InboundMessageAsync(venue));

        var queue = await GetQueueAsync(admin);

        var report = Assert.Single(queue, r => r.Id == reportId);
        Assert.Equal($"CR-{reportId}", report.Reference);
        Assert.Equal(InboundMessage, report.MessageExcerpt);
    }

    private async Task<int> SubmitReportAsync(HttpClient client, InboxMessage message)
    {
        var response = await client.PostAsync(
            $"/api/conversations/{message.ConversationId}/messages/{message.Id}/report",
            new { category = "illegalContent", details = "unlawful" });
        await response.ShouldBe(HttpStatusCode.NoContent);

        var admin = fixture.CreateClient(fixture.SeedState.Admin);
        return (await GetQueueAsync(admin)).Single(report => report.MessageId == message.Id).Id;
    }

    private static async Task<List<QueuedReport>> GetQueueAsync(HttpClient admin) =>
        (await (await admin.GetAsync("/api/Moderation/reports")).Content.ReadAsync<QueuePage>())!.Data;

    private static async Task<InboxMessage> InboundMessageAsync(HttpClient client) =>
        (await GetMessagesAsync(client)).Single(message => message.Content == InboundMessage);

    private static async Task<List<InboxMessage>> GetMessagesAsync(HttpClient client)
    {
        var previews = (await (await client.GetAsync("/api/conversations/previews"))
            .Content.ReadAsync<List<MessagePreview>>())!;
        var conversationId = Assert.Single(previews).ConversationId;
        return (await (await client.GetAsync($"/api/conversations/{conversationId}/messages"))
            .Content.ReadAsync<List<InboxMessage>>())!;
    }

    private static async Task<int> UnreadCountAsync(HttpClient client) =>
        await (await client.GetAsync("/api/conversations/unread-count")).Content.ReadAsync<int>();

    private sealed record QueuePage(List<QueuedReport> Data);
    private sealed record InboxMessage(int Id, int ConversationId, string Content);
    private sealed record MessagePreview(int ConversationId);
    private sealed record QueuedReport(
        int Id, string Reference, int MessageId, string MessageExcerpt,
        string? Outcome, DateTime? ResolvedAt, string? ResolutionNotes);
}
