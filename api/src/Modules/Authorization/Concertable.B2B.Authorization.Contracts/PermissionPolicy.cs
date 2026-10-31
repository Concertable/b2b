namespace Concertable.B2B.Authorization.Contracts;

public static class PermissionPolicy
{
    public const string Prefix = "perm:";

    public static string Name(TenantPermission permission) =>
        TenantPermission.All.Contains(permission)
            ? $"{Prefix}{permission.Key}"
            : throw new ArgumentOutOfRangeException(nameof(permission));

    public static bool TryParse(string? policyName, out TenantPermission permission)
    {
        permission = default;

        if (policyName is null || !policyName.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        return TenantPermission.TryParseKey(policyName[Prefix.Length..], out permission);
    }
}
