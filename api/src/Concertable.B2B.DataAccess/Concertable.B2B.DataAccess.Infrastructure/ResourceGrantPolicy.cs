using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.Infrastructure;

public static class ResourceGrantPolicy
{
    public static bool Allows<TGrant, TScope>(
        IEnumerable<TGrant> grants,
        TScope scope,
        MembershipSnapshot actor,
        ResourceAudience audience,
        DateTime at)
        where TGrant : ResourceAccessGrant<TScope>
        where TScope : struct, Enum =>
        grants.Any(grant =>
            EqualityComparer<TScope>.Default.Equals(grant.Scope, scope)
            && grant.TenantId == actor.TenantId
            && grant.IsLiveAt(at)
            && (audience == ResourceAudience.TenantResources
                    && (grant.MembershipId is null || grant.MembershipId == actor.MembershipId)
                || audience == ResourceAudience.AssignedResources
                    && grant.MembershipId == actor.MembershipId));
}
