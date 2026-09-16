using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class ThreadAccessGrant : ResourceAccessGrant<ThreadAccessScope>
{
    private ThreadAccessGrant() { }

    internal static ThreadAccessGrant Issue(
        int threadId,
        Guid tenantId,
        Guid? memberUserId,
        ThreadAccessScope scope,
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
            scope,
            issuedByTenantId,
            issuedByUserId,
            origin,
            at,
            validUntil);
        return grant;
    }
}
