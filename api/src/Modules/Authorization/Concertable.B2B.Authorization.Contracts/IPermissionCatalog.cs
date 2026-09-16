namespace Concertable.B2B.Authorization.Contracts;

public interface IPermissionCatalog
{
    bool Grants(TenantRole role, string permission);
}
