using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class ConcertAccessGrant : ResourceAccessGrant<ConcertAccessScope>
{
    private ConcertAccessGrant() { }

    internal static ConcertAccessGrant Issue(
        int concertId,
        Guid tenantId,
        Guid? membershipId,
        ConcertAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        ResourceGrantKind kind,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new ConcertAccessGrant();
        grant.Initialize(
            concertId,
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
