using System.Collections.Frozen;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

/// <summary>Role to permissions. What a business is, is eligibility decided by the owning operation, not a
/// second authority axis over the role bundle.</summary>
internal sealed class PermissionCatalog : IPermissionCatalog
{
    private static readonly FrozenDictionary<TenantRole, FrozenSet<string>> ByRole =
        new Dictionary<TenantRole, FrozenSet<string>>
        {
            [TenantRole.Owner] = new[]
            {
                TenantPermission.OperationsView, TenantPermission.ProfileEdit, TenantPermission.PayoutsManage,
                TenantPermission.SettlementView, TenantPermission.SettlementTrigger,
                TenantPermission.TenantSettingsEdit, TenantPermission.TenantDelete,
                TenantPermission.MembersInvite, TenantPermission.MembersRemove, TenantPermission.MembersManageRoles,
                TenantPermission.MessagesRead, TenantPermission.MessagesSend,
                TenantPermission.ConcertsOpsEdit, TenantPermission.ConcertsCheckIn,
                TenantPermission.ConcertsDeclareDoorRevenue,
                TenantPermission.OpportunitiesManage, TenantPermission.ApplicationsDecide,
                TenantPermission.ApplicationsSubmit, TenantPermission.ConcertsManage,
                TenantPermission.BookingsCancel, TenantPermission.TermsRead,
                TenantPermission.ResourcesShare,
            }.ToFrozenSet(),

            [TenantRole.Manager] = new[]
            {
                TenantPermission.OperationsView, TenantPermission.ProfileEdit,
                TenantPermission.SettlementView, TenantPermission.MembersInvite,
                TenantPermission.MessagesRead, TenantPermission.MessagesSend,
                TenantPermission.ConcertsOpsEdit, TenantPermission.ConcertsCheckIn,
                TenantPermission.ConcertsDeclareDoorRevenue,
                TenantPermission.OpportunitiesManage, TenantPermission.ApplicationsDecide,
                TenantPermission.ApplicationsSubmit, TenantPermission.ConcertsManage,
                TenantPermission.BookingsCancel, TenantPermission.TermsRead,
                TenantPermission.ResourcesShare,
            }.ToFrozenSet(),

            [TenantRole.Finance] = new[]
            {
                TenantPermission.OperationsView, TenantPermission.PayoutsManage,
                TenantPermission.SettlementView, TenantPermission.SettlementTrigger,
                TenantPermission.TermsRead, TenantPermission.MessagesRead,
            }.ToFrozenSet(),

            [TenantRole.Staff] = new[]
            {
                TenantPermission.OperationsView, TenantPermission.MessagesRead, TenantPermission.MessagesSend,
                TenantPermission.ConcertsOpsEdit, TenantPermission.ConcertsCheckIn,
            }.ToFrozenSet(),

            [TenantRole.Door] = new[]
            {
                TenantPermission.OperationsView, TenantPermission.ConcertsCheckIn,
            }.ToFrozenSet(),

            [TenantRole.Sound] = new[]
            {
                TenantPermission.OperationsView, TenantPermission.ConcertsOpsEdit,
            }.ToFrozenSet(),
        }.ToFrozenDictionary();

    public IReadOnlySet<string> For(TenantRole role) =>
        ByRole.TryGetValue(role, out var permissions) ? permissions : FrozenSet<string>.Empty;

    public bool Grants(TenantRole role, string permission) =>
        ByRole.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    public ResourceAudience AudienceFor(TenantRole role, string permission)
    {
        if (!Grants(role, permission))
            return ResourceAudience.None;

        return role switch
        {
            TenantRole.Owner or TenantRole.Manager or TenantRole.Finance => ResourceAudience.TenantResources,
            TenantRole.Staff or TenantRole.Door or TenantRole.Sound => ResourceAudience.AssignedResources,
            _ => ResourceAudience.None,
        };
    }

    /// <summary>Every permission granted to at least one role — the catalog-coverage test checks this against the declared constants.</summary>
    internal static IReadOnlySet<string> All { get; } = ByRole.Values.SelectMany(p => p).ToFrozenSet();
}
