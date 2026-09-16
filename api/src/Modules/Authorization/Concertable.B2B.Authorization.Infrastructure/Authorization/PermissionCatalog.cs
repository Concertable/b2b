using System.Collections.Frozen;

namespace Concertable.B2B.Authorization.Infrastructure.Authorization;

/// <summary>
/// Role to permissions, with no tenant type in the lookup. What a business is — a venue operator, an artist,
/// a promoter, or none of those — is eligibility for particular work, decided by the owning operation against
/// the tenant's active business profiles; it is not a second authority axis over the role bundle.
/// </summary>
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
                TenantPermission.OpportunitiesManage, TenantPermission.ApplicationsDecide,
                TenantPermission.ApplicationsSubmit, TenantPermission.ConcertsManage,
                TenantPermission.ResourcesShare,
            }.ToFrozenSet(),

            [TenantRole.Manager] = new[]
            {
                TenantPermission.OperationsView, TenantPermission.ProfileEdit,
                TenantPermission.SettlementView, TenantPermission.MembersInvite,
                TenantPermission.MessagesRead, TenantPermission.MessagesSend,
                TenantPermission.ConcertsOpsEdit, TenantPermission.ConcertsCheckIn,
                TenantPermission.OpportunitiesManage, TenantPermission.ApplicationsDecide,
                TenantPermission.ApplicationsSubmit, TenantPermission.ConcertsManage,
                TenantPermission.ResourcesShare,
            }.ToFrozenSet(),

            [TenantRole.Finance] = new[]
            {
                TenantPermission.OperationsView, TenantPermission.PayoutsManage,
                TenantPermission.SettlementView, TenantPermission.SettlementTrigger,
                TenantPermission.MessagesRead,
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

            [TenantRole.RestrictedParticipant] = new[]
            {
                TenantPermission.OperationsView,
            }.ToFrozenSet(),
        }.ToFrozenDictionary();

    public bool Grants(TenantRole role, string permission) =>
        ByRole.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    /// <summary>Every permission granted to at least one role — the catalog-coverage test checks this against the declared constants.</summary>
    internal static IReadOnlySet<string> All { get; } = ByRole.Values.SelectMany(p => p).ToFrozenSet();
}
