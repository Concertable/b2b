namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// Encodes/decodes the authorization policy name for <c>[HasPermission]</c>: <c>perm:&lt;permission&gt;</c>,
/// optionally suffixed <c>:&lt;persona&gt;</c>. The policy name carries everything the
/// <c>PermissionPolicyProvider</c> needs to rebuild the requirement on demand, so there is no startup
/// policy loop. Permission strings never contain a colon, which keeps the split unambiguous.
/// </summary>
public static class PermissionPolicy
{
    public const string Prefix = "perm:";

    public static string Name(string permission) => $"{Prefix}{permission}";

    public static string Name(string permission, TenantType persona) => $"{Prefix}{permission}:{persona}";

    /// <summary>Parses a <c>perm:</c> policy name into its permission and optional persona; <see langword="false"/> for any other name.</summary>
    public static bool TryParse(string policyName, out string permission, out TenantType? persona)
    {
        permission = string.Empty;
        persona = null;

        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
            return false;

        var rest = policyName[Prefix.Length..];
        var separator = rest.IndexOf(':');
        if (separator < 0)
        {
            permission = rest;
            return true;
        }

        permission = rest[..separator];
        persona = Enum.Parse<TenantType>(rest[(separator + 1)..]);
        return true;
    }
}
