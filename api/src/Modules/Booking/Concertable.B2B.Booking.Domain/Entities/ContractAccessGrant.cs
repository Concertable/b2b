using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Domain.Entities;

public sealed class ContractAccessGrant : ResourceAccessGrant<ContractAccessFacet>
{
    private ContractAccessGrant() { }

    internal static ContractAccessGrant Issue(
        int contractId,
        Guid tenantId,
        Guid? memberUserId,
        ContractAccessFacet facet,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        GrantOrigin origin,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new ContractAccessGrant();
        grant.Initialize(
            contractId,
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
