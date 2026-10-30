using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal static class ConversationGrantPolicy
{
    public static IQueryable<ConversationAccessGrant> Eligible(
        IQueryable<ConversationAccessGrant> grants,
        MembershipSnapshot actor,
        TenantPermission permission,
        ConversationAccessScope scope,
        DateTime now)
    {
        var audience = actor.AudienceFor(permission);
        return grants.Where(grant =>
            grant.TenantId == actor.TenantId
            && grant.Scope == scope
            && grant.RevokedAt == null
            && grant.ValidFrom <= now
            && (grant.ValidUntil == null || grant.ValidUntil > now)
            && (audience == ResourceAudience.TenantResources
                    && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                || audience == ResourceAudience.AssignedResources
                    && grant.MembershipId == actor.MembershipId));
    }

    public static IQueryable<ConversationAccessGrant> Principal(
        IQueryable<ConversationAccessGrant> grants,
        MembershipSnapshot actor,
        DateTime now) =>
        Eligible(grants, actor, TenantPermission.ResourcesShare, ConversationAccessScope.Read, now)
            .Where(grant => grant.Kind == ResourceGrantKind.Principal && grant.MembershipId == null);
}
