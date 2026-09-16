using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class ConcertAccessGrant : ResourceAccessGrant<ConcertAccessFacet>
{
    private ConcertAccessGrant() { }

    internal static ConcertAccessGrant Issue(
        int concertId,
        Guid tenantId,
        Guid? memberUserId,
        ConcertAccessFacet facet,
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
            facet,
            issuedByTenantId,
            issuedByUserId,
            origin,
            at,
            validUntil);
        return grant;
    }
}
