using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;
using Concertable.Kernel;

namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class ConversationEntity : IIdEntity
{
    private ConversationEntity() { }

    public int Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long LastMessageSequence { get; private set; }
    public long AccessVersion { get; private set; }

    private readonly List<ConversationParticipant> participants = [];
    public IReadOnlyList<ConversationParticipant> Participants => participants;

    private readonly List<MemberAssignment> assignments = [];
    public IReadOnlyList<MemberAssignment> Assignments => assignments;

    public static ConversationEntity Create(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid creatorTenantId,
        DateTime at)
    {
        ArgumentNullException.ThrowIfNull(participantTenantIds);
        var tenantIds = participantTenantIds.Distinct().ToList();
        if (tenantIds.Count < 2 || tenantIds.Contains(Guid.Empty) || !tenantIds.Contains(creatorTenantId))
            throw new ArgumentException("A conversation requires the creator and at least one other tenant.", nameof(participantTenantIds));

        var conversation = new ConversationEntity
        {
            CreatedAt = at,
            AccessVersion = 1
        };
        conversation.participants.AddRange(tenantIds.Select(tenantId => new ConversationParticipant(tenantId, at)));
        return conversation;
    }

    public long AllocateMessageSequence() => ++LastMessageSequence;

    public bool AssignMember(
        Guid actorTenantId,
        Guid assignedByUserId,
        Guid membershipId,
        Guid membershipTenantId,
        DateTime at)
    {
        if (membershipId == Guid.Empty
            || membershipTenantId != actorTenantId
            || !HasParticipant(actorTenantId)
            || IsAssigned(membershipId))
            return false;

        assignments.Add(new MemberAssignment(membershipId, assignedByUserId, at));
        AccessVersion++;
        return true;
    }

    public bool UnassignMember(Guid actorTenantId, Guid membershipId)
    {
        if (!HasParticipant(actorTenantId)
            || assignments.RemoveAll(assignment => assignment.MembershipId == membershipId) == 0)
            return false;

        AccessVersion++;
        return true;
    }

    public bool HasParticipant(Guid tenantId) =>
        participants.Exists(participant => participant.TenantId == tenantId);

    public bool IsReadableBy(MembershipSnapshot actor) =>
        Grants(actor, TenantPermission.MessagesRead);

    public bool AcceptsMessagesFrom(MembershipSnapshot actor) =>
        Grants(actor, TenantPermission.MessagesSend);

    public bool IsShareableBy(MembershipSnapshot actor) =>
        HasParticipant(actor.TenantId)
        && actor.Permissions[TenantPermission.ResourcesShare] == ResourceAccess.All;

    private bool Grants(MembershipSnapshot actor, TenantPermission permission) =>
        actor.Permissions[permission] switch
        {
            ResourceAccess.All => HasParticipant(actor.TenantId) || IsAssigned(actor.MembershipId),
            ResourceAccess.Assigned => IsAssigned(actor.MembershipId),
            _ => false,
        };

    private bool IsAssigned(Guid membershipId) =>
        assignments.Exists(assignment => assignment.MembershipId == membershipId);
}
