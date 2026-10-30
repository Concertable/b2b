using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.Infrastructure;

public static class ResourceGrantPolicy
{
    public static bool Allows<TGrant, TScope>(
        IEnumerable<TGrant> grants,
        TScope scope,
        MembershipSnapshot actor,
        TenantPermission permission,
        DateTime at)
        where TGrant : ResourceAccessGrant<TScope>
        where TScope : struct, Enum =>
        grants.AsQueryable().Any(ResourceAccessGrant<TScope>.IsLiveFor<TGrant>(actor, permission, at, scope));
}
