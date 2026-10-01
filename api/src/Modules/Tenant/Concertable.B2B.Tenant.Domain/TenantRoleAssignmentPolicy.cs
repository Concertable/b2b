using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;

namespace Concertable.B2B.Tenant.Domain;

public static class TenantRoleAssignmentPolicy
{
    public static bool CanAssign(
        MembershipSnapshot actor,
        bool isProtectedOwner,
        IReadOnlyCollection<TenantRoleDefinition> roles)
    {
        if (roles.Count == 0 || roles.Any(role => role.RetiredAt is not null
            || role.TenantId != actor.TenantId))
            return false;
        if (isProtectedOwner)
            return true;
        if (!actor.HasPermission(TenantPermission.MembersInvite))
            return false;
        return roles.All(role =>
            role.IsInvitationAssignable
            && !role.IsProtectedOwner
            && role.Permissions.All(grant =>
                TenantPermission.TryParse(grant.PermissionKey, out var permission)
                && permission != TenantPermission.MembersInvite
                && !AuthorizationCatalog.Permissions[permission].OwnerOnly
                && actor.AudienceFor(permission) >= grant.Audience));
    }
}
