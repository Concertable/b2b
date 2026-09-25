using System.Collections.Frozen;

namespace Concertable.B2B.Authorization.Contracts;

public readonly record struct TenantPermission
{
    public const string OperationsViewName = "operations.view";
    public const string ProfileEditName = "profile.edit";
    public const string PayoutsManageName = "payouts.manage";
    public const string SettlementViewName = "settlement.view";
    public const string SettlementTriggerName = "settlement.trigger";
    public const string TenantSettingsEditName = "tenant.settings.edit";
    public const string TenantDeleteName = "tenant.delete";
    public const string MembersInviteName = "members.invite";
    public const string MembersRemoveName = "members.remove";
    public const string MembersManageRolesName = "members.manage_roles";
    public const string MessagesReadName = "messages.read";
    public const string MessagesSendName = "messages.send";
    public const string ConcertsOpsEditName = "concerts.ops_edit";
    public const string ConcertsCheckInName = "concerts.check_in";
    public const string ConcertsDeclareDoorRevenueName = "concerts.declare_door_revenue";
    public const string OpportunitiesManageName = "opportunities.manage";
    public const string ApplicationsDecideName = "applications.decide";
    public const string ApplicationsSubmitName = "applications.submit";
    public const string ConcertsManageName = "concerts.manage";
    public const string BookingsCancelName = "bookings.cancel";
    public const string TermsReadName = "terms.read";
    public const string ResourcesShareName = "resources.share";

    public static TenantPermission OperationsView { get; } = new(OperationsViewName);
    public static TenantPermission ProfileEdit { get; } = new(ProfileEditName);
    public static TenantPermission PayoutsManage { get; } = new(PayoutsManageName);
    public static TenantPermission SettlementView { get; } = new(SettlementViewName);
    public static TenantPermission SettlementTrigger { get; } = new(SettlementTriggerName);
    public static TenantPermission TenantSettingsEdit { get; } = new(TenantSettingsEditName);
    public static TenantPermission TenantDelete { get; } = new(TenantDeleteName);
    public static TenantPermission MembersInvite { get; } = new(MembersInviteName);
    public static TenantPermission MembersRemove { get; } = new(MembersRemoveName);
    public static TenantPermission MembersManageRoles { get; } = new(MembersManageRolesName);
    public static TenantPermission MessagesRead { get; } = new(MessagesReadName);
    public static TenantPermission MessagesSend { get; } = new(MessagesSendName);
    public static TenantPermission ConcertsOpsEdit { get; } = new(ConcertsOpsEditName);
    public static TenantPermission ConcertsCheckIn { get; } = new(ConcertsCheckInName);
    public static TenantPermission ConcertsDeclareDoorRevenue { get; } = new(ConcertsDeclareDoorRevenueName);
    public static TenantPermission OpportunitiesManage { get; } = new(OpportunitiesManageName);
    public static TenantPermission ApplicationsDecide { get; } = new(ApplicationsDecideName);
    public static TenantPermission ApplicationsSubmit { get; } = new(ApplicationsSubmitName);
    public static TenantPermission ConcertsManage { get; } = new(ConcertsManageName);
    public static TenantPermission BookingsCancel { get; } = new(BookingsCancelName);
    public static TenantPermission TermsRead { get; } = new(TermsReadName);
    public static TenantPermission ResourcesShare { get; } = new(ResourcesShareName);

    public static IReadOnlySet<TenantPermission> All { get; } = new[]
    {
        OperationsView, ProfileEdit, PayoutsManage, SettlementView, SettlementTrigger,
        TenantSettingsEdit, TenantDelete, MembersInvite, MembersRemove, MembersManageRoles,
        MessagesRead, MessagesSend, ConcertsOpsEdit, ConcertsCheckIn, ConcertsDeclareDoorRevenue,
        OpportunitiesManage, ApplicationsDecide, ApplicationsSubmit, ConcertsManage, BookingsCancel,
        TermsRead, ResourcesShare,
    }.ToFrozenSet();

    private static readonly FrozenDictionary<string, TenantPermission> ByValue =
        All.ToFrozenDictionary(permission => permission.Value, StringComparer.Ordinal);

    private TenantPermission(string value)
    {
        this.Value = value;
    }

    public string Value { get; }

    public static bool TryParse(string? value, out TenantPermission permission)
    {
        if (value is not null && ByValue.TryGetValue(value, out permission))
            return true;

        permission = default;
        return false;
    }

    public override string ToString() => Value ?? string.Empty;
}
