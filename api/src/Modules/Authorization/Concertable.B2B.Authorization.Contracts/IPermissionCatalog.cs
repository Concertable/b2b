namespace Concertable.B2B.Authorization.Contracts;

public interface IPermissionCatalog
{
    bool Grants(TenantRole role, string permission);

    /// <summary>Everything the role carries, for a client that is told its permissions rather than deriving them.</summary>
    IReadOnlySet<string> For(TenantRole role);
}
