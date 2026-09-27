using System.Net;
using Xunit.Abstractions;

namespace Concertable.B2B.Conversations.IntegrationTests;

[Collection("Integration")]
public sealed class ContentReportApiTests : IAsyncLifetime
{
    private const string InboundMessage = "Test inbox message — artist to venue.";
    private const string OutboundMessage = "Test inbox message — venue to artist.";

    private readonly ConversationsApiFixture fixture;

    public ContentReportApiTests(ConversationsApiFixture fixture, ITestOutputHelper output)
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
    public async Task Report_ShouldReturn204_AndMailBothTheSafetyInboxAndTheReporter()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var inbound = await InboundMessageAsync(venue);

        var response = await venue.PostAsync(
            $"/api/conversations/{inbound.ConversationId}/messages/{inbound.Id}/report",
            new { category = "illegalContent", details = "This message is unlawful." });

        await response.ShouldBe(HttpStatusCode.NoContent);

        var safetyMail = Assert.Single(fixture.EmailSender.Sent, m => m.To == "safety@concertable.invalid");
        Assert.Contains("IllegalContent", safetyMail.Body, StringComparison.Ordinal);
        Assert.Contains(InboundMessage, safetyMail.Body, StringComparison.Ordinal);

        var reporterMail = Assert.Single(fixture.EmailSender.Sent,
            m => m.To == fixture.SeedState.VenueManager1.Email);
        Assert.Contains("CR-", reporterMail.Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Report_ShouldReturn404_WhenTheTenantIsNotPartyToTheConversation()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var inbound = await InboundMessageAsync(venue);
        var otherVenue = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await otherVenue.PostAsync(
            $"/api/conversations/{inbound.ConversationId}/messages/{inbound.Id}/report",
            new { category = "illegalContent", details = (string?)null });

        await response.ShouldBe(HttpStatusCode.NotFound);
        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task Report_ShouldReturn401_WhenAnonymous()
    {
        var anonymous = fixture.CreateClient();

        var response = await anonymous.PostAsync("/api/conversations/1/messages/1/report",
            new { category = "illegalContent" });

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Report_ShouldReturn400_WithFieldIndexedErrors_WhenDetailsAreTooLong()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var inbound = await InboundMessageAsync(venue);

        var response = await venue.PostAsync(
            $"/api/conversations/{inbound.ConversationId}/messages/{inbound.Id}/report",
            new { category = "illegalContent", details = new string('x', 2001) });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadAsync<ValidationProblem>();
        Assert.Contains("Details", problem!.Errors.Keys);
        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task Inbox_OffersTheReportLinkOnInboundOnly()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var messages = await GetMessagesAsync(venue);

        var inbound = messages.Single(m => m.Content == InboundMessage);
        Assert.NotNull(inbound.Actions.Report);
        Assert.Equal(
            $"/api/conversations/{inbound.ConversationId}/messages/{inbound.Id}/report",
            inbound.Actions.Report.Href);
        Assert.Equal("POST", inbound.Actions.Report.Method);

        var outbound = messages.Single(m => m.Content == OutboundMessage);
        Assert.Null(outbound.Actions.Report);
    }

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

    private sealed record InboxMessage(int Id, int ConversationId, string Content, InboxActions Actions);
    private sealed record MessagePreview(int ConversationId);
    private sealed record InboxActions(InboxActionLink? Report);
    private sealed record InboxActionLink(string Href, string Method);
    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
