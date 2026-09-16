namespace Concertable.B2B.Authorization.Contracts;

/// <summary>
/// Every permission a membership role can carry. One flat set: a permission is granted by role alone,
/// and whether the active tenant may run the operation at all is the owning operation's eligibility
/// policy — a tenant business profile, a resource grant, or both.
/// </summary>
public static class TenantPermission
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
    public const string ConcertsOpsEdit = "concerts.ops_edit";
    public const string ConcertsCheckIn = "concerts.check_in";
    public const string OpportunitiesManage = "opportunities.manage";
    public const string ApplicationsDecide = "applications.decide";
    public const string ApplicationsSubmit = "applications.submit";
    public const string ConcertsManage = "concerts.manage";
    public const string ResourcesShare = "resources.share";
}
