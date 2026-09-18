using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Domain.Entities;

public sealed class ContractAccessGrant : ResourceAccessGrant<ContractAccessScope>
{
    private ContractAccessGrant() { }

    internal static ContractAccessGrant Issue(
        int contractId,
        Guid tenantId,
        Guid? memberUserId,
        ContractAccessScope scope,
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
            scope,
            issuedByTenantId,
            issuedByUserId,
            origin,
            at,
            validUntil);
        return grant;
    }
}
