using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Application.Domain.Entities;

public sealed class ApplicationAccessGrant : ResourceAccessGrant<ApplicationAccessScope>
{
    private ApplicationAccessGrant() { }

    internal static ApplicationAccessGrant Issue(
        int applicationId,
        Guid tenantId,
        Guid? memberUserId,
        ApplicationAccessScope scope,
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
            scope,
            issuedByTenantId,
            issuedByUserId,
            origin,
            at,
            validUntil);
        return grant;
    }
}
