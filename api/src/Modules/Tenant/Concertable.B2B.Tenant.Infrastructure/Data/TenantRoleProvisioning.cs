namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal static class TenantRoleProvisioning
{
    public static IReadOnlyList<TenantRoleDefinition> CreatePresets(
        Guid tenantId, IEnumerable<TenantBusinessActivityKind> activities) =>
        SystemRoles.AvailableTo(activities)
            .Select(systemRole => TenantRoleDefinition.CreateSystemRole(tenantId, systemRole))
            .ToArray();
}
