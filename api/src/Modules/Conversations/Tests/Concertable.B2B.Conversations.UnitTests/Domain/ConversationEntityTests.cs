using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Conversations.UnitTests.Domain;

public sealed class ConversationEntityTests
{
    private static readonly DateTime At = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_RecordsEachTenantOnceAsAParticipant()
    {
        var creatorTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        var conversation = ConversationEntity.Create([creatorTenantId, otherTenantId, otherTenantId], creatorTenantId, At);

        Assert.Equal([creatorTenantId, otherTenantId], conversation.Participants.Select(participant => participant.TenantId));
        Assert.Empty(conversation.Assignments);
    }

    [Fact]
    public void Create_RejectsAnAudienceWithoutTheCreatorAndAnotherTenant()
    {
        var creatorTenantId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => ConversationEntity.Create([creatorTenantId], creatorTenantId, At));
        Assert.Throws<ArgumentException>(() => ConversationEntity.Create(
            [Guid.NewGuid(), Guid.NewGuid()], creatorTenantId, At));
    }

    [Fact]
    public void AllocateMessageSequence_IsMonotonic()
    {
        var creatorTenantId = Guid.NewGuid();
        var conversation = ConversationEntity.Create([creatorTenantId, Guid.NewGuid()], creatorTenantId, At);

        Assert.Equal(1, conversation.AllocateMessageSequence());
        Assert.Equal(2, conversation.AllocateMessageSequence());
    }

    [Fact]
    public void AssignMember_OnlyWithinTheParticipantTenantAndOnce()
    {
        var creatorTenantId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var conversation = ConversationEntity.Create([creatorTenantId, Guid.NewGuid()], creatorTenantId, At);

        Assert.False(conversation.AssignMember(creatorTenantId, Guid.NewGuid(), membershipId, Guid.NewGuid(), At));
        Assert.False(conversation.AssignMember(Guid.NewGuid(), Guid.NewGuid(), membershipId, Guid.NewGuid(), At));
        Assert.True(conversation.AssignMember(creatorTenantId, Guid.NewGuid(), membershipId, creatorTenantId, At));
        Assert.False(conversation.AssignMember(creatorTenantId, Guid.NewGuid(), membershipId, creatorTenantId, At));
        Assert.Equal(membershipId, Assert.Single(conversation.Assignments).MembershipId);
        Assert.Equal(2, conversation.AccessVersion);
    }

    [Fact]
    public void UnassignMember_RemovesTheAssignment()
    {
        var creatorTenantId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var conversation = ConversationEntity.Create([creatorTenantId, Guid.NewGuid()], creatorTenantId, At);
        conversation.AssignMember(creatorTenantId, Guid.NewGuid(), membershipId, creatorTenantId, At);

        Assert.True(conversation.UnassignMember(creatorTenantId, membershipId));
        Assert.Empty(conversation.Assignments);
        Assert.False(conversation.UnassignMember(creatorTenantId, membershipId));
    }

    [Theory]
    [InlineData(ResourceAccess.All, true, false, true)]
    [InlineData(ResourceAccess.All, false, false, false)]
    [InlineData(ResourceAccess.Assigned, true, false, false)]
    [InlineData(ResourceAccess.Assigned, false, true, true)]
    [InlineData(ResourceAccess.None, true, true, false)]
    public void IsReadableBy_ParticipantNeedsAllAndAssigneeNeedsAny(
        ResourceAccess access, bool participant, bool assigned, bool expected)
    {
        var participantTenantId = Guid.NewGuid();
        var conversation = ConversationEntity.Create([participantTenantId, Guid.NewGuid()], participantTenantId, At);
        var actor = Actor(participant ? participantTenantId : Guid.NewGuid(), TenantPermission.MessagesRead, access);
        if (assigned)
            conversation.AssignMember(participantTenantId, Guid.NewGuid(), actor.MembershipId, participantTenantId, At);

        Assert.Equal(expected, conversation.IsReadableBy(actor));
    }

    [Fact]
    public void AcceptsMessagesFrom_UsesTheSendPermission()
    {
        var tenantId = Guid.NewGuid();
        var conversation = ConversationEntity.Create([tenantId, Guid.NewGuid()], tenantId, At);

        Assert.True(conversation.AcceptsMessagesFrom(Actor(tenantId, TenantPermission.MessagesSend, ResourceAccess.All)));
        Assert.False(conversation.AcceptsMessagesFrom(Actor(tenantId, TenantPermission.MessagesRead, ResourceAccess.All)));
    }

    [Theory]
    [InlineData(ResourceAccess.All, true)]
    [InlineData(ResourceAccess.Assigned, false)]
    public void IsShareableBy_RequiresAParticipantWithShareAtAll(ResourceAccess access, bool expected)
    {
        var tenantId = Guid.NewGuid();
        var conversation = ConversationEntity.Create([tenantId, Guid.NewGuid()], tenantId, At);

        Assert.Equal(expected, conversation.IsShareableBy(Actor(tenantId, TenantPermission.ResourcesShare, access)));
        Assert.False(conversation.IsShareableBy(Actor(Guid.NewGuid(), TenantPermission.ResourcesShare, ResourceAccess.All)));
    }

    private static MembershipSnapshot Actor(Guid tenantId, TenantPermission permission, ResourceAccess access) =>
        new(Guid.NewGuid(), tenantId, Guid.NewGuid(), 1, 1,
            EffectivePermissions.From([KeyValuePair.Create(permission, access)]));
}
