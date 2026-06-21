using System.Collections.Frozen;

namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// The permissions and role bundles BOTH personas share (design §1.3) — defined once and composed into
/// <see cref="VenuePermissions"/> and <see cref="ArtistPermissions"/>, never duplicated across them. A venue
/// edits a venue profile and an artist an artist profile, but "edit my profile" is one shared permission;
/// the persona-specific surface is pinned at the controller, not here.
/// </summary>
public sealed class SharedPermissions : IPermissionSet
{
    public const string OperationsView = "operations.view";
    public const string ProfileEdit = "profile.edit";
    public const string PayoutsManage = "payouts.manage";
    public const string SettlementView = "settlement.view";
    public const string SettlementTrigger = "settlement.trigger";
    public const string TenantSettingsEdit = "tenant.settings.edit";
    public const string TenantDelete = "tenant.delete";
    public const string MembersInvite = "members.invite";
    public const string MembersRemove = "members.remove";
    public const string MembersManageRoles = "members.manage_roles";
    public const string MessagesRead = "messages.read";
    public const string MessagesSend = "messages.send";

    // Reserved — defined now so the six-role matrix is meaningful; first enforced when day-of-show ships.
    public const string ConcertsOpsEdit = "concerts.ops_edit";

    private static readonly FrozenDictionary<TenantRole, FrozenSet<string>> ByRole =
        new Dictionary<TenantRole, FrozenSet<string>>
        {
            [TenantRole.Owner] = new[]
            {
                OperationsView, ProfileEdit, PayoutsManage, SettlementView, SettlementTrigger,
                TenantSettingsEdit, TenantDelete, MembersInvite, MembersRemove, MembersManageRoles,
                MessagesRead, MessagesSend, ConcertsOpsEdit,
            }.ToFrozenSet(),

            [TenantRole.Manager] = new[]
            {
                OperationsView, ProfileEdit, SettlementView, MembersInvite, MessagesRead, MessagesSend,
                ConcertsOpsEdit,
            }.ToFrozenSet(),

            [TenantRole.Finance] = new[]
            {
                OperationsView, PayoutsManage, SettlementView, SettlementTrigger, MessagesRead,
            }.ToFrozenSet(),

            [TenantRole.Staff] = new[]
            {
                OperationsView, MessagesRead, MessagesSend, ConcertsOpsEdit,
            }.ToFrozenSet(),

            [TenantRole.Door] = new[] { OperationsView }.ToFrozenSet(),

            [TenantRole.Sound] = new[] { OperationsView, ConcertsOpsEdit }.ToFrozenSet(),
        }.ToFrozenDictionary();

    public bool Grants(TenantRole role, string permission) =>
        ByRole.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    /// <summary>Every shared permission granted to at least one role — the catalog-coverage test checks this against the declared constants.</summary>
    public static IReadOnlySet<string> All { get; } = ByRole.Values.SelectMany(p => p).ToFrozenSet();
}
