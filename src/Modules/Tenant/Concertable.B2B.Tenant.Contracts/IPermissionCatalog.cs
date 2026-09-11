namespace Concertable.B2B.Tenant.Contracts;

public interface IPermissionCatalog
{
    bool Grants(TenantType tenantType, TenantRole role, string permission);
}
