namespace Concertable.B2B.Tenant.Domain;

public static class TenantRoleAssignmentPolicy
{
    public static bool CanAssignRole(TenantRole actor, TenantRole target) =>
        actor == TenantRole.Owner
        || actor == TenantRole.Manager
        && target is TenantRole.Staff or TenantRole.Door or TenantRole.Sound;
}
