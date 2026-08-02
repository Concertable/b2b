namespace Concertable.B2B.Tenant.Contracts;

/// <summary>Provides role-to-permission grants for one tenant type.</summary>
public interface IPermissionSet
{
    /// <summary>True iff <paramref name="role"/>'s bundle in this set contains <paramref name="permission"/>.</summary>
    bool Grants(TenantRole role, string permission);
}
