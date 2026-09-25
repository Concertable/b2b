namespace Concertable.B2B.Authorization.Contracts;

public interface IPermissionCatalog
{
    bool Grants(TenantRole role, TenantPermission permission);

    IReadOnlySet<TenantPermission> For(TenantRole role);

    ResourceAudience AudienceFor(TenantRole role, TenantPermission permission);
}
