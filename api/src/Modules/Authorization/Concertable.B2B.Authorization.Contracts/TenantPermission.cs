using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace Concertable.B2B.Authorization.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<TenantPermission>))]
public enum TenantPermission
{
    [JsonStringEnumMemberName("applications:decide")]
    ApplicationsDecide = 1,
    [JsonStringEnumMemberName("applications:submit")]
    ApplicationsSubmit,
    [JsonStringEnumMemberName("bookings:cancel")]
    BookingsCancel,
    [JsonStringEnumMemberName("concerts:check_in")]
    ConcertsCheckIn,
    [JsonStringEnumMemberName("concerts:declare_door_revenue")]
    ConcertsDeclareDoorRevenue,
    [JsonStringEnumMemberName("concerts:manage")]
    ConcertsManage,
    [JsonStringEnumMemberName("concerts:ops_edit")]
    ConcertsOpsEdit,
    [JsonStringEnumMemberName("concerts:publish")]
    ConcertsPublish,
    [JsonStringEnumMemberName("members:invite")]
    MembersInvite,
    [JsonStringEnumMemberName("members:manage_roles")]
    MembersManageRoles,
    [JsonStringEnumMemberName("members:remove")]
    MembersRemove,
    [JsonStringEnumMemberName("messages:read")]
    MessagesRead,
    [JsonStringEnumMemberName("messages:send")]
    MessagesSend,
    [JsonStringEnumMemberName("operations:view")]
    OperationsView,
    [JsonStringEnumMemberName("opportunities:manage")]
    OpportunitiesManage,
    [JsonStringEnumMemberName("payouts:manage")]
    PayoutsManage,
    [JsonStringEnumMemberName("profile:edit")]
    ProfileEdit,
    [JsonStringEnumMemberName("resources:share")]
    ResourcesShare,
    [JsonStringEnumMemberName("settlement:trigger")]
    SettlementTrigger,
    [JsonStringEnumMemberName("settlement:view")]
    SettlementView,
    [JsonStringEnumMemberName("tenant:delete")]
    TenantDelete,
    [JsonStringEnumMemberName("tenant_settings:edit")]
    TenantSettingsEdit,
    [JsonStringEnumMemberName("terms:read")]
    TermsRead,
}

public static class TenantPermissionExtensions
{
    private static readonly FrozenDictionary<TenantPermission, string> Names =
        new Dictionary<TenantPermission, string>
        {
            [TenantPermission.ApplicationsDecide] = "applications:decide",
            [TenantPermission.ApplicationsSubmit] = "applications:submit",
            [TenantPermission.BookingsCancel] = "bookings:cancel",
            [TenantPermission.ConcertsCheckIn] = "concerts:check_in",
            [TenantPermission.ConcertsDeclareDoorRevenue] = "concerts:declare_door_revenue",
            [TenantPermission.ConcertsManage] = "concerts:manage",
            [TenantPermission.ConcertsOpsEdit] = "concerts:ops_edit",
            [TenantPermission.ConcertsPublish] = "concerts:publish",
            [TenantPermission.MembersInvite] = "members:invite",
            [TenantPermission.MembersManageRoles] = "members:manage_roles",
            [TenantPermission.MembersRemove] = "members:remove",
            [TenantPermission.MessagesRead] = "messages:read",
            [TenantPermission.MessagesSend] = "messages:send",
            [TenantPermission.OperationsView] = "operations:view",
            [TenantPermission.OpportunitiesManage] = "opportunities:manage",
            [TenantPermission.PayoutsManage] = "payouts:manage",
            [TenantPermission.ProfileEdit] = "profile:edit",
            [TenantPermission.ResourcesShare] = "resources:share",
            [TenantPermission.SettlementTrigger] = "settlement:trigger",
            [TenantPermission.SettlementView] = "settlement:view",
            [TenantPermission.TenantDelete] = "tenant:delete",
            [TenantPermission.TenantSettingsEdit] = "tenant_settings:edit",
            [TenantPermission.TermsRead] = "terms:read",
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, TenantPermission> ByName =
        Names.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    private static readonly FrozenSet<TenantPermission> Declared = Names.Keys.ToFrozenSet();

    private static readonly FrozenDictionary<TenantPermission, string> Labels =
        new Dictionary<TenantPermission, string>
        {
            [TenantPermission.ApplicationsDecide] = "Decide applications",
            [TenantPermission.ApplicationsSubmit] = "Submit applications",
            [TenantPermission.BookingsCancel] = "Cancel bookings",
            [TenantPermission.ConcertsCheckIn] = "Check in attendees",
            [TenantPermission.ConcertsDeclareDoorRevenue] = "Declare door revenue",
            [TenantPermission.ConcertsManage] = "Manage concerts",
            [TenantPermission.ConcertsOpsEdit] = "Edit concert operations",
            [TenantPermission.ConcertsPublish] = "Publish concerts",
            [TenantPermission.MembersInvite] = "Invite members",
            [TenantPermission.MembersManageRoles] = "Manage member roles",
            [TenantPermission.MembersRemove] = "Remove members",
            [TenantPermission.MessagesRead] = "Read messages",
            [TenantPermission.MessagesSend] = "Send messages",
            [TenantPermission.OperationsView] = "View operations",
            [TenantPermission.OpportunitiesManage] = "Manage opportunities",
            [TenantPermission.PayoutsManage] = "Manage payouts",
            [TenantPermission.ProfileEdit] = "Edit business profiles",
            [TenantPermission.ResourcesShare] = "Share resources",
            [TenantPermission.SettlementTrigger] = "Trigger settlement",
            [TenantPermission.SettlementView] = "View settlement",
            [TenantPermission.TenantDelete] = "Delete organization",
            [TenantPermission.TenantSettingsEdit] = "Edit organization settings",
            [TenantPermission.TermsRead] = "Read agreement terms",
        }.ToFrozenDictionary();

    private static readonly FrozenDictionary<TenantPermission, string> Categories =
        new Dictionary<TenantPermission, string>
        {
            [TenantPermission.ApplicationsDecide] = "Application",
            [TenantPermission.ApplicationsSubmit] = "Application",
            [TenantPermission.BookingsCancel] = "Booking",
            [TenantPermission.ConcertsCheckIn] = "Concert",
            [TenantPermission.ConcertsDeclareDoorRevenue] = "Concert",
            [TenantPermission.ConcertsManage] = "Concert",
            [TenantPermission.ConcertsOpsEdit] = "Concert",
            [TenantPermission.ConcertsPublish] = "Concert",
            [TenantPermission.MembersInvite] = "Tenant",
            [TenantPermission.MembersManageRoles] = "Tenant",
            [TenantPermission.MembersRemove] = "Tenant",
            [TenantPermission.MessagesRead] = "Conversations",
            [TenantPermission.MessagesSend] = "Conversations",
            [TenantPermission.OperationsView] = "Tenant",
            [TenantPermission.OpportunitiesManage] = "Opportunity",
            [TenantPermission.PayoutsManage] = "Tenant",
            [TenantPermission.ProfileEdit] = "Tenant",
            [TenantPermission.ResourcesShare] = "Concert",
            [TenantPermission.SettlementTrigger] = "Tenant",
            [TenantPermission.SettlementView] = "Tenant",
            [TenantPermission.TenantDelete] = "Tenant",
            [TenantPermission.TenantSettingsEdit] = "Tenant",
            [TenantPermission.TermsRead] = "Booking",
        }.ToFrozenDictionary();

    private static readonly FrozenSet<TenantPermission> OwnerOnlyPermissions =
        new HashSet<TenantPermission>
        {
            TenantPermission.MembersManageRoles,
            TenantPermission.MembersRemove,
            TenantPermission.TenantDelete,
        }.ToFrozenSet();

    extension(TenantPermission permission)
    {
        public string Name => Names.TryGetValue(permission, out var name) ? name : string.Empty;
        public string Label => Labels.TryGetValue(permission, out var label) ? label : string.Empty;
        public string Category => Categories.TryGetValue(permission, out var category) ? category : string.Empty;
        public bool IsOwnerOnly => OwnerOnlyPermissions.Contains(permission);
    }

    extension(TenantPermission)
    {
        public static IReadOnlySet<TenantPermission> All => Declared;

        public static bool TryParseName(string? name, out TenantPermission permission)
        {
            if (name is not null && ByName.TryGetValue(name, out permission))
                return true;
            permission = default;
            return false;
        }
    }
}
