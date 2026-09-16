using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class InvoiceAccessGrant : ResourceAccessGrant<InvoiceAccessFacet>
{
    private InvoiceAccessGrant() { }

    internal static InvoiceAccessGrant Issue(
        int invoiceId,
        Guid tenantId,
        Guid? memberUserId,
        InvoiceAccessFacet facet,
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
            facet,
            issuedByTenantId,
            issuedByUserId,
            origin,
            at,
            validUntil);
        return grant;
    }
}
