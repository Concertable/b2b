namespace Concertable.B2B.Authorization.Contracts;

public static class PermissionPolicy
{
    public const string Prefix = "perm:";

    public static string Name(TenantPermission permission)
    {
        if (!TenantPermission.TryParse(permission.Value, out var declared) || declared != permission)
            throw new ArgumentOutOfRangeException(nameof(permission));

        return $"{Prefix}{permission.Value}";
    }

    public static bool TryParse(string? policyName, out TenantPermission permission)
    {
        permission = default;

        if (policyName is null || !policyName.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        return TenantPermission.TryParse(policyName[Prefix.Length..], out permission);
    }
}
