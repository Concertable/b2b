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
        var messageId = await InboundMessageIdAsync(venue);

        var response = await venue.PostAsync($"/api/Message/{messageId}/report",
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
    public async Task Report_ShouldReturn404_WhenTheTenantIsNotPartyToTheThread()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var messageId = await InboundMessageIdAsync(venue);
        var otherVenue = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await otherVenue.PostAsync($"/api/Message/{messageId}/report",
            new { category = "illegalContent", details = (string?)null });

        await response.ShouldBe(HttpStatusCode.NotFound);
        Assert.Empty(fixture.EmailSender.Sent);
    }

    [Fact]
    public async Task Report_ShouldReturn401_WhenAnonymous()
    {
        var anonymous = fixture.CreateClient();

        var response = await anonymous.PostAsync("/api/Message/1/report",
            new { category = "illegalContent" });

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // FluentValidation auto-validation rejects before the action runs, so the key is ModelState's
    // property name — dictionary keys are not camel-cased by JsonSerializerDefaults.Web.
    [Fact]
    public async Task Report_ShouldReturn400_WithFieldIndexedErrors_WhenDetailsAreTooLong()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var messageId = await InboundMessageIdAsync(venue);

        var response = await venue.PostAsync($"/api/Message/{messageId}/report",
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

        var page = await GetInboxAsync(venue);

        var inbound = page.Data.Single(m => m.Content == InboundMessage);
        Assert.NotNull(inbound.Actions.Report);
        Assert.Equal($"/api/Message/{inbound.Id}/report", inbound.Actions.Report.Href);
        Assert.Equal("POST", inbound.Actions.Report.Method);

        var outbound = page.Data.Single(m => m.Content == OutboundMessage);
        Assert.Null(outbound.Actions.Report);
    }

    private static async Task<int> InboundMessageIdAsync(HttpClient client) =>
        (await GetInboxAsync(client)).Data.Single(m => m.Content == InboundMessage).Id;

    private static async Task<InboxPage> GetInboxAsync(HttpClient client) =>
        (await (await client.GetAsync("/api/Message/user")).Content.ReadAsync<InboxPage>())!;

    private sealed record InboxPage(List<InboxMessage> Data);
    private sealed record InboxMessage(int Id, string Content, InboxActions Actions);
    private sealed record InboxActions(InboxActionLink? Report);
    private sealed record InboxActionLink(string Href, string Method);
    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);
}
