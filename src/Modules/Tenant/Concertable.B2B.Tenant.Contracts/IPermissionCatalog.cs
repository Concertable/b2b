namespace Concertable.B2B.Tenant.Contracts;

/// <summary>Resolves role-to-permission grants for a tenant type.</summary>
public interface IPermissionCatalog
{
    /// <summary>Returns whether the role is granted the permission for the tenant type.</summary>
    bool Grants(TenantType tenantType, TenantRole role, string permission);
}
