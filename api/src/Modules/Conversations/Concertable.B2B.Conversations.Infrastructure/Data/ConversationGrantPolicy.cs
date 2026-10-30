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
        return grants.Where(
            ResourceAccessGrant<ConversationAccessScope>.IsLiveFor<ConversationAccessGrant>(actor, permission, now, scope));
    }

    public static IQueryable<ConversationAccessGrant> Principal(
        IQueryable<ConversationAccessGrant> grants,
        MembershipSnapshot actor,
        DateTime now) =>
        Eligible(grants, actor, TenantPermission.ResourcesShare, ConversationAccessScope.Read, now)
            .Where(grant => grant.Kind == ResourceGrantKind.Principal && grant.MembershipId == null);
}
