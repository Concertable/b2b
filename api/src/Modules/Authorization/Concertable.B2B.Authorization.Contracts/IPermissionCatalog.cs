namespace Concertable.B2B.Authorization.Contracts;

public interface IPermissionCatalog
{
    bool Grants(TenantRole role, string permission);

    /// <summary>Everything the role carries, for a client that is told its permissions rather than deriving them.</summary>
    IReadOnlySet<string> For(TenantRole role);

    /// <summary>
    /// How wide the role's hold on that one permission reaches. Evaluated per requested operation, never once
    /// for the whole request: the same role reads tenant-wide for one permission and only its assigned
    /// resources for another.
    /// </summary>
    ResourceAudience AudienceFor(TenantRole role, string permission);
}
