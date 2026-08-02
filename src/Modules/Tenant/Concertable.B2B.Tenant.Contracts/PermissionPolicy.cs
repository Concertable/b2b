namespace Concertable.B2B.Tenant.Contracts;

public static class PermissionPolicy
{
    public const string Prefix = "perm:";

    public static string Name(string permission) => $"{Prefix}{permission}";

    /// <summary>Parses a <c>perm:</c> policy name into its permission; <see langword="false"/> for any other name.</summary>
    public static bool TryParse(string policyName, out string permission)
    {
        permission = string.Empty;

        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        permission = policyName[Prefix.Length..];
        return true;
    }
}
