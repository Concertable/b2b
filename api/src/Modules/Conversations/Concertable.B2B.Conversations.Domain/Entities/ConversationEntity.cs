using Concertable.B2B.Conversations.Contracts.Enums;
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

    private readonly List<ConversationAccessGrant> accessGrants = [];
    public IReadOnlyList<ConversationAccessGrant> AccessGrants => accessGrants;

    public static ConversationEntity Create(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid creatorTenantId,
        Guid createdByUserId,
        DateTime at)
    {
        ArgumentNullException.ThrowIfNull(participantTenantIds);
        var participants = participantTenantIds.Distinct().ToList();
        if (participants.Count < 2 || participants.Contains(Guid.Empty) || !participants.Contains(creatorTenantId))
            throw new ArgumentException("A conversation requires the creator and at least one other tenant.", nameof(participantTenantIds));

        var conversation = new ConversationEntity
        {
            CreatedAt = at,
            AccessVersion = 1
        };
        foreach (var tenantId in participants)
            conversation.AdmitPrincipal(tenantId, creatorTenantId, createdByUserId, at);

        return conversation;
    }

    public long AllocateMessageSequence() => ++LastMessageSequence;

    public bool AssignMember(Guid actorTenantId, Guid membershipId, Guid membershipTenantId, DateTime at)
    {
        if (membershipId == Guid.Empty
            || membershipTenantId != actorTenantId
            || !IsPrincipal(actorTenantId, at))
            return false;

        var changed = false;
        foreach (var scope in new[] { ConversationAccessScope.Read, ConversationAccessScope.SendMessages })
        {
            if (accessGrants.Exists(grant =>
                    grant.TenantId == actorTenantId
                    && grant.MembershipId == membershipId
                    && grant.Scope == scope
                    && grant.IsLiveAt(at)))
                continue;

            accessGrants.Add(ConversationAccessGrant.Issue(
                Id,
                actorTenantId,
                membershipId,
                scope,
                issuedByTenantId: actorTenantId,
                issuedByUserId: null,
                ResourceGrantKind.MemberAssignment,
                at));
            changed = true;
        }

        if (changed)
            AccessVersion++;
        return changed;
    }

    public bool RemoveMemberAssignment(Guid actorTenantId, Guid membershipId, DateTime at)
    {
        if (!IsPrincipal(actorTenantId, at))
            return false;

        var grants = accessGrants
            .Where(grant =>
                grant.TenantId == actorTenantId
                && grant.MembershipId == membershipId
                && grant.Kind == ResourceGrantKind.MemberAssignment
                && grant.IsLiveAt(at))
            .ToList();
        foreach (var grant in grants)
            grant.Revoke(at);

        if (grants.Count > 0)
            AccessVersion++;
        return grants.Count > 0;
    }

    public bool Admits(Guid tenantId, ConversationAccessScope scope, DateTime at) =>
        accessGrants.Exists(grant =>
            grant.TenantId == tenantId && grant.Scope == scope && grant.IsLiveAt(at));

    private void AdmitPrincipal(Guid tenantId, Guid issuedByTenantId, Guid issuedByUserId, DateTime at)
    {
        foreach (var scope in new[] { ConversationAccessScope.Read, ConversationAccessScope.SendMessages })
        {
            accessGrants.Add(ConversationAccessGrant.Issue(
                Id,
                tenantId,
                membershipId: null,
                scope,
                issuedByTenantId,
                issuedByUserId,
                ResourceGrantKind.Principal,
                at));
        }
    }

    private bool IsPrincipal(Guid tenantId, DateTime at) =>
        accessGrants.Exists(grant =>
            grant.TenantId == tenantId
            && grant.Kind == ResourceGrantKind.Principal
            && grant.Scope == ConversationAccessScope.Read
            && grant.IsLiveAt(at));
}
