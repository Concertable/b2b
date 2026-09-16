using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class InvoiceAccessGrant : ResourceAccessGrant<InvoiceAccessScope>
{
    private InvoiceAccessGrant() { }

    internal static InvoiceAccessGrant Issue(
        int invoiceId,
        Guid tenantId,
        Guid? memberUserId,
        InvoiceAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        GrantOrigin origin,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new InvoiceAccessGrant();
        grant.Initialize(
            invoiceId,
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
