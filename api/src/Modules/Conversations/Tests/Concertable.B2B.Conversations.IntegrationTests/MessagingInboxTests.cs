using System.Net;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Contracts.Enums;
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
    public async Task Conversation_ReturnsEveryParticipantAndSequencedMessages()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var preview = Assert.Single(await GetPreviewsAsync(venue));

        var conversation = await (await venue.GetAsync($"/api/conversations/{preview.ConversationId}"))
            .Content.ReadAsync<Conversation>();
        var messages = await GetMessagesAsync(venue, preview.ConversationId);

        Assert.Equal(
            new[]
            {
                TenantSeedIds.For(fixture.SeedState.VenueManager1.Id),
                TenantSeedIds.For(fixture.SeedState.ArtistManager1.Id)
            }.Order().ToArray(),
            conversation!.Participants.Select(participant => participant.TenantId).Order().ToArray());
        Assert.Equal([1L, 2L], messages.Select(message => message.Sequence).ToArray());
        Assert.Equal("Test inbox message — artist to venue.", messages[0].Content);
        Assert.Equal("Test inbox message — venue to artist.", messages[1].Content);
    }

    [Fact]
    public async Task Conversation_IsAbsentForATenantOutsideItsAudience()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var conversationId = Assert.Single(await GetPreviewsAsync(venue)).ConversationId;
        var otherVenue = fixture.CreateClient(fixture.SeedState.VenueManager2);

        await (await otherVenue.GetAsync($"/api/conversations/{conversationId}"))
            .ShouldBe(HttpStatusCode.NotFound);
        await (await otherVenue.GetAsync($"/api/conversations/{conversationId}/messages"))
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadPosition_AdvancesThroughAnExistingSequence()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var conversationId = Assert.Single(await GetPreviewsAsync(venue)).ConversationId;

        Assert.Equal(1, await GetUnreadCountAsync(venue));
        await (await venue.PutAsync(
                $"/api/conversations/{conversationId}/read-position",
                new { throughSequence = 2 }))
            .ShouldBe(HttpStatusCode.NoContent);
        Assert.Equal(0, await GetUnreadCountAsync(venue));

        await (await venue.PutAsync(
                $"/api/conversations/{conversationId}/read-position",
                new { throughSequence = 99 }))
            .ShouldBe(HttpStatusCode.BadRequest);
        Assert.Equal(0, await GetUnreadCountAsync(venue));
    }

    [Fact]
    public async Task Create_ReplaysTheSameRequestAndRejectsAChangedPayload()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var requestId = Guid.NewGuid();
        var venueTenantId = TenantSeedIds.For(fixture.SeedState.VenueManager1.Id);
        var artistTenantId = TenantSeedIds.For(fixture.SeedState.ArtistManager1.Id);

        var first = await CreateAsync(venue, requestId, venueTenantId, artistTenantId);
        var replay = await CreateAsync(venue, requestId, venueTenantId, artistTenantId);
        Assert.Equal(first.ConversationId, replay.ConversationId);

        await (await venue.PostAsync("/api/conversations", new
            {
                requestId,
                participantTenantIds = new[]
                {
                    venueTenantId,
                    TenantSeedIds.For(fixture.SeedState.ArtistManagers[1].Id)
                }
            }))
            .ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DistinctCreateRequests_MayCreateSeparateConversationsForTheSameAudience()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var venueTenantId = TenantSeedIds.For(fixture.SeedState.VenueManager1.Id);
        var artistTenantId = TenantSeedIds.For(fixture.SeedState.ArtistManager1.Id);

        var first = await CreateAsync(venue, Guid.NewGuid(), venueTenantId, artistTenantId);
        var second = await CreateAsync(venue, Guid.NewGuid(), venueTenantId, artistTenantId);

        Assert.NotEqual(first.ConversationId, second.ConversationId);
    }

    [Fact]
    public async Task Send_ReplaysTheSameMessageAndRejectsChangedContent()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var conversation = await CreateAsync(
            venue,
            Guid.NewGuid(),
            TenantSeedIds.For(fixture.SeedState.VenueManager1.Id),
            TenantSeedIds.For(fixture.SeedState.ArtistManager1.Id));
        var requestId = Guid.NewGuid();

        var first = await SendAsync(venue, conversation.ConversationId, requestId, "First content");
        var replay = await SendAsync(venue, conversation.ConversationId, requestId, "First content");
        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(first.Sequence, replay.Sequence);

        await (await venue.PostAsync($"/api/conversations/{conversation.ConversationId}/messages", new
            {
                requestId,
                content = "Changed content"
            }))
            .ShouldBe(HttpStatusCode.Conflict);
        Assert.Single(await GetMessagesAsync(venue, conversation.ConversationId));
    }

    [Fact]
    public async Task Previews_UseParticipantListsAndFallBackToTheNewestVisibleMessage()
    {
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var admin = fixture.CreateClient(fixture.SeedState.Admin);
        var preview = Assert.Single(await GetPreviewsAsync(venue));
        Assert.Equal(2, preview.Participants.Count);
        Assert.Equal("Test inbox message — venue to artist.", preview.Preview);
        Assert.False(preview.Unread);
        Assert.Equal("/?inbox=open", preview.Href);

        var latest = (await GetMessagesAsync(venue, preview.ConversationId)).Single(message =>
            message.Content == "Test inbox message — venue to artist.");
        await admin.PostAsync($"/api/Moderation/messages/{latest.Id}/hide");
        await venue.PutAsync(
            $"/api/conversations/{preview.ConversationId}/read-position",
            new { throughSequence = 1 });

        var visible = Assert.Single(await GetPreviewsAsync(venue));
        Assert.Equal("Test inbox message — artist to venue.", visible.Preview);
        Assert.False(visible.Unread);
    }

    [Fact]
    public async Task AssignedStaff_StaleRemovalPreservesTheNewerGrant()
    {
        var owner = fixture.SeedState.VenueManager1;
        var staff = fixture.SeedState.VenueManager3;
        var tenantId = TenantSeedIds.For(owner.Id);
        var membership = fixture.SeedState.Memberships.Single(value =>
            value.TenantId == tenantId && value.UserId == staff.Id);
        var ownerClient = fixture.CreateClient(owner);
        var staffClient = fixture.CreateClient(staff);
        staffClient.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());

        await (await ownerClient.PutAsync(
                $"/api/organization/members/{staff.Id}/role",
                new { role = TenantRole.Staff.ToString() }))
            .ShouldBe(HttpStatusCode.NoContent);

        var preview = Assert.Single(await GetPreviewsAsync(ownerClient));
        var before = await GetConversationAsync(ownerClient, preview.ConversationId);
        await (await staffClient.GetAsync($"/api/conversations/{preview.ConversationId}"))
            .ShouldBe(HttpStatusCode.NotFound);

        await (await ownerClient.PostAsync(
                $"/api/conversations/{preview.ConversationId}/member-assignments",
                new
                {
                    membershipId = membership.Id,
                    expectedAccessVersion = before.AccessVersion
                }))
            .ShouldBe(HttpStatusCode.NoContent);

        var assigned = await GetConversationAsync(ownerClient, preview.ConversationId);
        await (await staffClient.GetAsync($"/api/conversations/{preview.ConversationId}"))
            .ShouldBe(HttpStatusCode.OK);
        await SendAsync(staffClient, preview.ConversationId, Guid.NewGuid(), "Assigned staff message");

        await (await ownerClient.DeleteAsync(
                $"/api/conversations/{preview.ConversationId}/member-assignments/{membership.Id}?expectedVersion={before.AccessVersion}"))
            .ShouldBe(HttpStatusCode.Conflict);
        await (await staffClient.GetAsync($"/api/conversations/{preview.ConversationId}"))
            .ShouldBe(HttpStatusCode.OK);

        await (await ownerClient.DeleteAsync(
                $"/api/conversations/{preview.ConversationId}/member-assignments/{membership.Id}?expectedVersion={assigned.AccessVersion}"))
            .ShouldBe(HttpStatusCode.NoContent);
        await (await staffClient.GetAsync($"/api/conversations/{preview.ConversationId}"))
            .ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<Conversation> CreateAsync(
        HttpClient client,
        Guid requestId,
        params Guid[] participantTenantIds)
    {
        var response = await client.PostAsync("/api/conversations", new { requestId, participantTenantIds });
        await response.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadAsync<Conversation>())!;
    }

    private static async Task<Message> SendAsync(
        HttpClient client,
        int conversationId,
        Guid requestId,
        string content)
    {
        var response = await client.PostAsync(
            $"/api/conversations/{conversationId}/messages",
            new { requestId, content });
        await response.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadAsync<Message>())!;
    }

    private static async Task<List<MessagePreview>> GetPreviewsAsync(HttpClient client) =>
        (await (await client.GetAsync("/api/conversations/previews"))
            .Content.ReadAsync<List<MessagePreview>>())!;

    private static async Task<List<Message>> GetMessagesAsync(HttpClient client, int conversationId) =>
        (await (await client.GetAsync($"/api/conversations/{conversationId}/messages"))
            .Content.ReadAsync<List<Message>>())!;

    private static async Task<Conversation> GetConversationAsync(HttpClient client, int conversationId) =>
        (await (await client.GetAsync($"/api/conversations/{conversationId}"))
            .Content.ReadAsync<Conversation>())!;

    private static async Task<int> GetUnreadCountAsync(HttpClient client) =>
        await (await client.GetAsync("/api/conversations/unread-count")).Content.ReadAsync<int>();

    private sealed record Conversation(
        int ConversationId,
        long AccessVersion,
        List<ConversationParticipant> Participants);
    private sealed record ConversationParticipant(Guid TenantId, string DisplayName);
    private sealed record Message(
        int Id,
        int ConversationId,
        long Sequence,
        Guid SenderTenantId,
        Guid SentByUserId,
        string Content,
        DateTime SentAt);
    private sealed record MessagePreview(
        int Id,
        int ConversationId,
        List<ConversationParticipant> Participants,
        string Preview,
        DateTime At,
        bool Unread,
        string Href);
}
