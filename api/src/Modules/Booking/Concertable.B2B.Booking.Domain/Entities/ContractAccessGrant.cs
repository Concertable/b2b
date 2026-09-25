using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Booking.Domain.Entities;

public sealed class ContractAccessGrant : ResourceAccessGrant<ContractAccessScope>
{
    private ContractAccessGrant() { }

    internal static ContractAccessGrant Issue(
        int contractId,
        Guid tenantId,
        Guid? membershipId,
        ContractAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        ResourceGrantKind kind,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new ContractAccessGrant();
        grant.Initialize(
            contractId,
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
