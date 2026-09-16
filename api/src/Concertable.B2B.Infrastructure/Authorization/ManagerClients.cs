using System.Collections.Frozen;
using Concertable.Auth.Contracts;
using Concertable.B2B.Tenant.Contracts.Enums;

namespace Concertable.B2B.Infrastructure.Authorization;

/// <summary>
/// B2B's own assignment at registration: which Auth interactive clients are B2B manager clients, which of
/// them provision a business tenant, and which marketplace profile (if any) that tenant starts with. Auth's
/// <see cref="InteractiveClient"/> is identity-only and carries no business opinion — deciding who becomes a
/// manager is B2B's authorization decision, not Auth's. The single source both
/// <c>CredentialRegisteredHandler</c> and <c>TenantProvisioningHandler</c> read, so the two classifications
/// can never drift apart. A provisioning client with no entry here creates the legal tenant and membership
/// with no profile activated, which is the ordinary shape for an agency or production business.
/// </summary>
public static class ManagerClients
{
    private static readonly FrozenSet<InteractiveClient> Manager = new[]
    {
        InteractiveClient.VenueBrowser,
        InteractiveClient.VenueMobile,
        InteractiveClient.ArtistBrowser,
        InteractiveClient.ArtistMobile,
        InteractiveClient.Admin,
    }.ToFrozenSet();

    private static readonly FrozenDictionary<InteractiveClient, TenantBusinessProfileKind> InitialProfiles =
        new Dictionary<InteractiveClient, TenantBusinessProfileKind>
        {
            [InteractiveClient.VenueBrowser] = TenantBusinessProfileKind.VenueOperator,
            [InteractiveClient.VenueMobile] = TenantBusinessProfileKind.VenueOperator,
            [InteractiveClient.ArtistBrowser] = TenantBusinessProfileKind.Artist,
            [InteractiveClient.ArtistMobile] = TenantBusinessProfileKind.Artist,
        }.ToFrozenDictionary();

    extension(InteractiveClient client)
    {
        /// <summary>True for a B2B manager client — venue, artist or platform admin.</summary>
        public bool IsManagerClient => Manager.Contains(client);

        /// <summary>Whether registering on this client provisions a business tenant. The platform admin does not.</summary>
        public bool ProvisionsBusinessTenant => Manager.Contains(client) && client != InteractiveClient.Admin;

        /// <summary>The marketplace profile the provisioned tenant activates, or none.</summary>
        public TenantBusinessProfileKind? InitialBusinessProfile =>
            InitialProfiles.TryGetValue(client, out var kind) ? kind : null;
    }
}
