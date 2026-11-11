using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.UnitTests.Domain;

public sealed class ConversationEntityTests
{
    [Fact]
    public void Create_IssuesOnlyExplicitReadAndSendGrantsForEachPrincipal()
    {
        var creatorTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        var conversation = ConversationEntity.Create(
            [creatorTenantId, otherTenantId], creatorTenantId, Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(4, conversation.AccessGrants.Count);
        Assert.All(conversation.AccessGrants, grant => Assert.Equal(ResourceGrantKind.Principal, grant.Kind));
        Assert.Equal(
            [ConversationAccessScope.Read, ConversationAccessScope.SendMessages],
            conversation.AccessGrants.Where(grant => grant.TenantId == creatorTenantId)
                .Select(grant => grant.Scope).Order().ToArray());
        Assert.Equal(
            [ConversationAccessScope.Read, ConversationAccessScope.SendMessages],
            conversation.AccessGrants.Where(grant => grant.TenantId == otherTenantId)
                .Select(grant => grant.Scope).Order().ToArray());
    }

    [Fact]
    public void Create_RejectsAnAudienceWithoutTheCreatorAndAnotherTenant()
    {
        var creatorTenantId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => ConversationEntity.Create(
            [creatorTenantId], creatorTenantId, Guid.NewGuid(), DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => ConversationEntity.Create(
            [Guid.NewGuid(), Guid.NewGuid()], creatorTenantId, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void AllocateMessageSequence_IsMonotonic()
    {
        var creatorTenantId = Guid.NewGuid();
        var conversation = ConversationEntity.Create(
            [creatorTenantId, Guid.NewGuid()], creatorTenantId, Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(1, conversation.AllocateMessageSequence());
        Assert.Equal(2, conversation.AllocateMessageSequence());
    }

    [Fact]
    public void AssignMember_IssuesBothScopesOnlyWithinThePrincipalTenant()
    {
        var creatorTenantId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var at = DateTime.UtcNow;
        var conversation = ConversationEntity.Create(
            [creatorTenantId, Guid.NewGuid()], creatorTenantId, Guid.NewGuid(), at);

        Assert.Empty(conversation.AssignMember(creatorTenantId, membershipId, Guid.NewGuid(), at));
        Assert.Equal(2, conversation.AssignMember(creatorTenantId, membershipId, creatorTenantId, at).Count);

        var assignments = conversation.AccessGrants
            .Where(grant => grant.MembershipId == membershipId).ToList();
        Assert.Equal(2, assignments.Count);
        Assert.All(assignments, grant => Assert.Equal(ResourceGrantKind.MemberAssignment, grant.Kind));
        Assert.Equal(
            [ConversationAccessScope.Read, ConversationAccessScope.SendMessages],
            assignments.Select(grant => grant.Scope).Order().ToArray());
    }
}
