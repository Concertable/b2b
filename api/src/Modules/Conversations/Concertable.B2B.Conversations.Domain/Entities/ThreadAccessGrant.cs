using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Conversations.Domain.Entities;

public sealed class ThreadAccessGrant : ResourceAccessGrant<ThreadAccessScope>
{
    private ThreadAccessGrant() { }

    internal static ThreadAccessGrant Issue(
        int threadId,
        Guid tenantId,
        Guid? membershipId,
        ThreadAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        ResourceGrantKind kind,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new ThreadAccessGrant();
        grant.Initialize(
            threadId,
            tenantId,
            membershipId,
            scope,
            issuedByTenantId,
            issuedByUserId,
            kind,
            at,
            validUntil);
        return grant;
    }

    internal void Revoke(DateTime at) => RevokeCore(at);
}
