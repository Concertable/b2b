namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

internal sealed class TenantAuthorizer(
    IMembershipContext membership,
    IMembershipReadRepository memberships,
    IRoleRepository roles)
{
    public async Task<Result<MembershipSnapshot, AccessDenied>> AuthorizeAsync(
        Guid tenantId, TenantPermission permission, bool ownerRequired, CancellationToken ct = default)
    {
        if (membership.Membership is not { } expected
            || expected.TenantId != tenantId
            || await memberships.LockIfUnchangedAsync(expected, ct) is not { } actor
            || !actor.HasPermission(permission)
            || ownerRequired && !await roles.HasProtectedOwnerAsync(tenantId, actor.MembershipId, ct))
            return new AccessDenied();

        return actor;
    }
}
