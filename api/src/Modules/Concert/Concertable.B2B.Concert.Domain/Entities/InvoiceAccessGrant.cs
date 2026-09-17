using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class InvoiceAccessGrant : ResourceAccessGrant<InvoiceAccessScope>
{
    private InvoiceAccessGrant() { }

    internal static InvoiceAccessGrant Issue(
        int invoiceId,
        Guid tenantId,
        Guid? membershipId,
        InvoiceAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        ResourceGrantKind kind,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new InvoiceAccessGrant();
        grant.Initialize(
            invoiceId,
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
