using System.Collections.Frozen;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

internal sealed class PermissionCatalog : IPermissionCatalog
{
    private static readonly FrozenDictionary<TenantRole, FrozenSet<TenantPermission>> ByRole =
        new Dictionary<TenantRole, FrozenSet<TenantPermission>>
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

    public IReadOnlySet<TenantPermission> For(TenantRole role) =>
        ByRole.TryGetValue(role, out var permissions) ? permissions : FrozenSet<TenantPermission>.Empty;

    public bool Grants(TenantRole role, TenantPermission permission) =>
        ByRole.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    public ResourceAudience AudienceFor(TenantRole role, TenantPermission permission)
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

    internal static IReadOnlySet<TenantPermission> All { get; } = ByRole.Values.SelectMany(p => p).ToFrozenSet();
}
