using System.Collections.Frozen;
using Concertable.Auth.Contracts;
using Concertable.B2B.Tenant.Contracts.Enums;

namespace Concertable.B2B.Infrastructure.Authorization;

/// <summary>
/// B2B's own role assignment at registration: which Auth interactive clients are B2B manager clients, and
/// which <see cref="TenantType"/> (if any) each provisions. Auth's <see cref="InteractiveClient"/> is
/// identity-only and carries no business-party opinion — deciding who becomes a venue/artist/admin manager
/// is B2B's authorization decision, not Auth's. The single source both <c>CredentialRegisteredHandler</c>
/// and <c>TenantProvisioningHandler</c> read, so the two classifications can never drift apart. Sibling to
/// <c>Tenant.Infrastructure.Authorization.PermissionAuthorizationHandler</c> — that one checks a
/// permission on an already-authenticated request; this one assigns the initial role at registration.
/// </summary>
public static class ManagerClients
{
    private static readonly FrozenDictionary<InteractiveClient, TenantType?> TenantTypeByClient =
        new Dictionary<InteractiveClient, TenantType?>
        {
            [InteractiveClient.VenueBrowser] = TenantType.Venue,
            [InteractiveClient.VenueMobile] = TenantType.Venue,
            [InteractiveClient.ArtistBrowser] = TenantType.Artist,
            [InteractiveClient.ArtistMobile] = TenantType.Artist,
            [InteractiveClient.Admin] = null,
        }.ToFrozenDictionary();

    extension(InteractiveClient client)
    {
        /// <summary>True for a B2B manager client — venue, artist or platform admin.</summary>
        public bool IsManagerClient => TenantTypeByClient.ContainsKey(client);

        /// <summary>The tenant type a manager client provisions, or <see langword="null"/> for a manager
        /// client that provisions none (the platform admin) or a non-manager client.</summary>
        public TenantType? ManagerTenantType => TenantTypeByClient.GetValueOrDefault(client);
    }
}
