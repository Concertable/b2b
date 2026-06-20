using System.Collections.Frozen;

namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// The static role→permission matrix (design §1.3) — the single source of truth for what each
/// <see cref="TenantRole"/> may do. A code-defined map, not a <c>RolePermission</c> table: unit-testable,
/// versioned with code, no admin UI, no per-tenant custom roles, and it doesn't fight the re-scaffold
/// migration convention. Membership rows store only the role; expansion to permissions happens here.
/// Persona scope is enforced at the call-site (see <see cref="Permissions"/>), so the catalog is persona-blind.
/// </summary>
public static class PermissionCatalog
{
    private static readonly FrozenDictionary<TenantRole, FrozenSet<string>> ByRole =
        new Dictionary<TenantRole, FrozenSet<string>>
        {
            [TenantRole.Owner] = new[]
            {
                Permissions.OperationsView, Permissions.ProfileEdit, Permissions.OpportunitiesManage,
                Permissions.ApplicationsDecide, Permissions.ApplicationsSubmit, Permissions.ConcertsManage,
                Permissions.PayoutsManage, Permissions.SettlementView, Permissions.SettlementTrigger,
                Permissions.TenantSettingsEdit, Permissions.TenantDelete, Permissions.MembersInvite,
                Permissions.MembersRemove, Permissions.MembersManageRoles, Permissions.MessagesRead,
                Permissions.MessagesSend, Permissions.ConcertsOpsEdit, Permissions.ConcertsCheckIn,
            }.ToFrozenSet(),

            [TenantRole.Manager] = new[]
            {
                Permissions.OperationsView, Permissions.ProfileEdit, Permissions.OpportunitiesManage,
                Permissions.ApplicationsDecide, Permissions.ApplicationsSubmit, Permissions.ConcertsManage,
                Permissions.SettlementView, Permissions.MembersInvite, Permissions.MessagesRead,
                Permissions.MessagesSend, Permissions.ConcertsOpsEdit, Permissions.ConcertsCheckIn,
            }.ToFrozenSet(),

            [TenantRole.Finance] = new[]
            {
                Permissions.OperationsView, Permissions.PayoutsManage, Permissions.SettlementView,
                Permissions.SettlementTrigger, Permissions.MessagesRead,
            }.ToFrozenSet(),

            [TenantRole.Staff] = new[]
            {
                Permissions.OperationsView, Permissions.MessagesRead, Permissions.MessagesSend,
                Permissions.ConcertsOpsEdit, Permissions.ConcertsCheckIn,
            }.ToFrozenSet(),

            [TenantRole.Door] = new[]
            {
                Permissions.OperationsView, Permissions.ConcertsCheckIn,
            }.ToFrozenSet(),

            [TenantRole.Sound] = new[]
            {
                Permissions.OperationsView, Permissions.ConcertsOpsEdit,
            }.ToFrozenSet(),
        }.ToFrozenDictionary();

    /// <summary>True iff <paramref name="role"/>'s bundle contains <paramref name="permission"/>.</summary>
    public static bool Grants(TenantRole role, string permission) =>
        ByRole.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    /// <summary>Every permission granted to at least one role — the catalog-coverage test checks this against <see cref="Permissions"/>.</summary>
    public static IReadOnlySet<string> All { get; } = ByRole.Values.SelectMany(p => p).ToFrozenSet();
}
