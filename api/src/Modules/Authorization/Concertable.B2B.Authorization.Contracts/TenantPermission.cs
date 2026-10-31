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
    private static readonly FrozenDictionary<TenantPermission, string> Keys =
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

    private static readonly FrozenDictionary<string, TenantPermission> ByKey =
        Keys.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    private static readonly FrozenSet<TenantPermission> Declared = Keys.Keys.ToFrozenSet();

    extension(TenantPermission permission)
    {
        public string Key => Keys.TryGetValue(permission, out var key) ? key : string.Empty;
    }

    extension(TenantPermission)
    {
        public static IReadOnlySet<TenantPermission> All => Declared;

        public static bool TryParseKey(string? key, out TenantPermission permission)
        {
            if (key is not null && ByKey.TryGetValue(key, out permission))
                return true;
            permission = default;
            return false;
        }
    }
}
