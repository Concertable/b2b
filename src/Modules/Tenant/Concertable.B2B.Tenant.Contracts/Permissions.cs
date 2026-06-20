namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// The permission catalog as <see langword="string"/> constants — the idiomatic modern ASP.NET Core shape
/// where the identifier <em>is</em> the policy name behind <c>[HasPermission(...)]</c>. Call-sites check
/// permissions, never role names, so reshaping a role touches <see cref="PermissionCatalog"/> alone. The
/// identifier never crosses a serialization boundary (tokens are identity-only; membership rows store the
/// role, not permissions), so the string form costs nothing an enum would have saved — the lost compile-time
/// check is recovered by the catalog-coverage test. Persona scope ((V)/(A) in the design matrix) is pinned at
/// the call-site via the attribute's persona argument, not stored here.
/// </summary>
public static class Permissions
{
    public const string OperationsView = "operations.view";
    public const string ProfileEdit = "profile.edit";
    public const string OpportunitiesManage = "opportunities.manage";
    public const string ApplicationsDecide = "applications.decide";
    public const string ApplicationsSubmit = "applications.submit";
    public const string ConcertsManage = "concerts.manage";
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
    public const string ConcertsCheckIn = "concerts.check_in";
}
