using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Application.Domain.Entities;

public sealed class ApplicationAccessGrant : ResourceAccessGrant<ApplicationAccessFacet>
{
    private ApplicationAccessGrant() { }

    internal static ApplicationAccessGrant Issue(
        int applicationId,
        Guid tenantId,
        Guid? memberUserId,
        ApplicationAccessFacet facet,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        GrantOrigin origin,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new ApplicationAccessGrant();
        grant.Initialize(
            applicationId,
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
