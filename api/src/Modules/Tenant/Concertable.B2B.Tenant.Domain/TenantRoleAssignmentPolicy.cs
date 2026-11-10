using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;

namespace Concertable.B2B.Tenant.Domain;

public static class TenantRoleAssignmentPolicy
{
    public static bool AreAvailableForTenant(TenantEntity tenant, IReadOnlyCollection<TenantRoleDefinition> roles)
    {
        var activeActivities = tenant.BusinessActivities
            .Where(activity => activity.IsActive)
            .Select(activity => activity.Kind)
            .ToHashSet();
        return roles.All(role =>
            role.SystemRole is not { } systemRole
            || systemRole.RequiredActivity is not { } required
            || activeActivities.Contains(required));
    }

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
                TenantPermission.TryParseName(grant.PermissionKey, out var permission)
                && permission != TenantPermission.MembersInvite
                && !permission.IsOwnerOnly
                && actor.Permissions[permission] >= grant.Access));
    }
}
