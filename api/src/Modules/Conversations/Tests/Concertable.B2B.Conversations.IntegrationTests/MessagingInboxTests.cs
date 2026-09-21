using System.Net;
using System.Net.Http.Json;
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
    public async Task Create_ConcurrentSameRequest_ReplaysTheWinningConversation()
    {
        var actor = fixture.SeedState.VenueManager1;
        var client = fixture.CreateClient(actor);
        var requestId = Guid.NewGuid();
        var creatorTenantId = TenantSeedIds.For(actor.Id);
        var createdByMembershipId = fixture.SeedState.Memberships.Single(membership =>
            membership.TenantId == creatorTenantId && membership.UserId == actor.Id).Id;
        var participantTenantIds = new[]
        {
            creatorTenantId,
            TenantSeedIds.For(fixture.SeedState.ArtistManager1.Id)
        };

        var responses = await fixture.RunWithConversationCreationBarrierAsync(
            creatorTenantId,
            createdByMembershipId,
            requestId,
            ct => new[]
            {
                client.PostAsJsonAsync("/api/conversations", new { requestId, participantTenantIds }, ct),
                client.PostAsJsonAsync("/api/conversations", new { requestId, participantTenantIds }, ct)
            });

        foreach (var response in responses)
            await response.ShouldBe(HttpStatusCode.Created);
        var conversations = await Task.WhenAll(
            responses.Select(response => response.Content.ReadAsync<Conversation>()));
        Assert.Equal(conversations[0]!.ConversationId, conversations[1]!.ConversationId);
        foreach (var response in responses)
            response.Dispose();
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
    public async Task AssignedStaff_AccessIsMembershipScopedAndVersioned()
    {
        var owner = fixture.SeedState.VenueManager1;
        var staff = fixture.SeedState.VenueManager3;
        var tenantId = TenantSeedIds.For(owner.Id);
        var membership = fixture.SeedState.Memberships.Single(value =>
            value.TenantId == tenantId && value.UserId == staff.Id);
        var foreignTenantId = TenantSeedIds.For(fixture.SeedState.VenueManager2.Id);
        var foreignMembership = fixture.SeedState.Memberships.Single(value =>
            value.TenantId == foreignTenantId && value.UserId == fixture.SeedState.VenueManager2.Id);
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
        await (await staffClient.PostAsync(
                $"/api/conversations/{preview.ConversationId}/messages",
                new { requestId = Guid.NewGuid(), content = "Unassigned staff message" }))
            .ShouldBe(HttpStatusCode.Forbidden);

        await (await ownerClient.PostAsync(
                $"/api/conversations/{preview.ConversationId}/member-assignments",
                new
                {
                    membershipId = foreignMembership.Id,
                    expectedAccessVersion = before.AccessVersion
                }))
            .ShouldBe(HttpStatusCode.BadRequest);
        Assert.Equal(
            before.AccessVersion,
            (await GetConversationAsync(ownerClient, preview.ConversationId)).AccessVersion);

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
                $"/api/conversations/{preview.ConversationId}/member-assignments/{membership.Id}"))
            .ShouldBe(HttpStatusCode.BadRequest);
        Assert.Equal(
            assigned.AccessVersion,
            (await GetConversationAsync(ownerClient, preview.ConversationId)).AccessVersion);
        await (await staffClient.GetAsync($"/api/conversations/{preview.ConversationId}"))
            .ShouldBe(HttpStatusCode.OK);

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
        await (await staffClient.PostAsync(
                $"/api/conversations/{preview.ConversationId}/messages",
                new { requestId = Guid.NewGuid(), content = "Removed staff message" }))
            .ShouldBe(HttpStatusCode.Forbidden);

        var removed = await GetConversationAsync(ownerClient, preview.ConversationId);
        await (await ownerClient.PostAsync(
                $"/api/conversations/{preview.ConversationId}/member-assignments",
                new
                {
                    membershipId = membership.Id,
                    expectedAccessVersion = removed.AccessVersion
                }))
            .ShouldBe(HttpStatusCode.NoContent);
        await (await staffClient.GetAsync($"/api/conversations/{preview.ConversationId}"))
            .ShouldBe(HttpStatusCode.OK);

        await (await ownerClient.DeleteAsync($"/api/organization/members/{staff.Id}"))
            .ShouldBe(HttpStatusCode.NoContent);
        var invitationResponse = await ownerClient.PostAsync(
            "/api/organization/invitations",
            new { staff.Email, role = TenantRole.Staff.ToString() });
        await invitationResponse.ShouldBe(HttpStatusCode.Created);
        var invitation = (await invitationResponse.Content.ReadAsync<Invitation>())!;
        var acceptanceResponse = await fixture.CreateClient(staff)
            .PostAsync($"/api/invitation/{invitation.Id}/accept");
        await acceptanceResponse.ShouldBe(HttpStatusCode.OK);
        var rejoinedMembership = (await acceptanceResponse.Content.ReadAsync<Membership>())!;
        Assert.NotEqual(membership.Id, rejoinedMembership.MembershipId);

        var rejoinedClient = fixture.CreateClient(staff);
        rejoinedClient.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());
        await (await rejoinedClient.GetAsync($"/api/conversations/{preview.ConversationId}"))
            .ShouldBe(HttpStatusCode.NotFound);
        await (await rejoinedClient.PostAsync(
                $"/api/conversations/{preview.ConversationId}/messages",
                new { requestId = Guid.NewGuid(), content = "Rejoined staff message" }))
            .ShouldBe(HttpStatusCode.Forbidden);
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
    private sealed record Invitation(Guid Id);
    private sealed record Membership(Guid MembershipId);
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
