namespace Concertable.B2B.Tenant.Contracts;

public interface IPermissionSet
{
    bool Grants(TenantRole role, string permission);
}
