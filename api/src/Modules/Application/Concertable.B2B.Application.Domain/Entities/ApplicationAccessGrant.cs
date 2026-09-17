using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Application.Domain.Entities;

public sealed class ApplicationAccessGrant : ResourceAccessGrant<ApplicationAccessScope>
{
    private ApplicationAccessGrant() { }

    internal static ApplicationAccessGrant Issue(
        int applicationId,
        Guid tenantId,
        Guid? membershipId,
        ApplicationAccessScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        ResourceGrantKind kind,
        DateTime at,
        DateTime? validUntil = null)
    {
        var grant = new ApplicationAccessGrant();
        grant.Initialize(
            applicationId,
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
