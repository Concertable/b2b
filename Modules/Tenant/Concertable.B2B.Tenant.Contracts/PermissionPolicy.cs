namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// Encodes/decodes the authorization policy name for <c>[HasPermission]</c>: <c>perm:&lt;permission&gt;</c>.
/// The policy name carries everything the <c>PermissionPolicyProvider</c> needs to rebuild the requirement on
/// demand, so there is no startup policy loop. Persona is not encoded here — the catalog enforces a
/// permission's persona by construction, and a controller's surface persona comes from
/// <see cref="TenantPersonaAttribute"/>.
/// </summary>
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
