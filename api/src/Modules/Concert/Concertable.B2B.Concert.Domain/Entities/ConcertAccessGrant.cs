using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class ConcertAccessGrant : ResourceAccessGrant<ConcertAccessScope>
{
    private ConcertAccessGrant() { }

    internal static ConcertAccessGrant Issue(
        int concertId,
        Guid tenantId,
        Guid? memberUserId,
        ConcertAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        GrantOrigin origin,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new ConcertAccessGrant();
        grant.Initialize(
            concertId,
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
