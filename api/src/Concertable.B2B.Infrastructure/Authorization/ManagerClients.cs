using System.Collections.Frozen;
using Concertable.Auth.Contracts;
using Concertable.B2B.Tenant.Contracts.Enums;

namespace Concertable.B2B.Infrastructure.Authorization;

public static class ManagerClients
{
    private static readonly FrozenSet<InteractiveClient> Manager = new[]
    {
        InteractiveClient.VenueBrowser,
        InteractiveClient.VenueMobile,
        InteractiveClient.ArtistBrowser,
        InteractiveClient.ArtistMobile,
        InteractiveClient.BusinessBrowser,
        InteractiveClient.BusinessMobile,
        InteractiveClient.Admin,
    }.ToFrozenSet();

    private static readonly FrozenDictionary<InteractiveClient, TenantBusinessActivityKind> InitialActivities =
        new Dictionary<InteractiveClient, TenantBusinessActivityKind>
        {
            [InteractiveClient.VenueBrowser] = TenantBusinessActivityKind.VenueOperator,
            [InteractiveClient.VenueMobile] = TenantBusinessActivityKind.VenueOperator,
            [InteractiveClient.ArtistBrowser] = TenantBusinessActivityKind.Artist,
            [InteractiveClient.ArtistMobile] = TenantBusinessActivityKind.Artist,
        }.ToFrozenDictionary();

    extension(InteractiveClient client)
    {
        public bool IsManagerClient => Manager.Contains(client);

        public bool ProvisionsBusinessTenant => InitialActivities.ContainsKey(client);

        public TenantBusinessActivityKind? InitialBusinessActivity =>
            InitialActivities.TryGetValue(client, out var kind) ? kind : null;
    }
}
