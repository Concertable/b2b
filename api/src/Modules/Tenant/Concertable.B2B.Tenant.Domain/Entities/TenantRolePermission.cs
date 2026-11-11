using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class TenantRolePermission
{
    private TenantRolePermission() { }

    public Guid TenantId { get; private set; }
    public Guid RoleId { get; private set; }
    public string PermissionKey { get; private set; } = null!;
    public ResourceAccess Access { get; private set; }

    internal void ChangeAccess(ResourceAccess access) => Access = access;

    internal static TenantRolePermission Create(
        Guid tenantId, Guid roleId, TenantPermission permission, ResourceAccess access) =>
        new()
        {
            TenantId = tenantId,
            RoleId = roleId,
            PermissionKey = permission.Name,
            Access = access,
        };
}
