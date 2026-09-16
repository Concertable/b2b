using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class ThreadAccessGrant : ResourceAccessGrant<ThreadAccessFacet>
{
    private ThreadAccessGrant() { }

    internal static ThreadAccessGrant Issue(
        int threadId,
        Guid tenantId,
        Guid? memberUserId,
        ThreadAccessFacet facet,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        GrantOrigin origin,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new ThreadAccessGrant();
        grant.Initialize(
            threadId,
            tenantId,
            memberUserId,
            facet,
            issuedByTenantId,
            issuedByUserId,
            origin,
            at,
            validUntil);
        return grant;
    }
}
