using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Tenant.Contracts;

public enum SystemRole
{
    Owner = 1,
    Manager,
    Finance,
    Staff,
    Door,
    Sound,
}

public static class SystemRoleExtensions
{
    private static readonly FrozenDictionary<SystemRole, IReadOnlyDictionary<TenantPermission, ResourceAccess>> GrantsByRole =
        new Dictionary<SystemRole, IReadOnlyDictionary<TenantPermission, ResourceAccess>>
        {
            [SystemRole.Owner] = new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.ApplicationsDecide] = ResourceAccess.All,
                [TenantPermission.ApplicationsSubmit] = ResourceAccess.All,
                [TenantPermission.BookingsCancel] = ResourceAccess.All,
                [TenantPermission.ConcertsCheckIn] = ResourceAccess.All,
                [TenantPermission.ConcertsDeclareDoorRevenue] = ResourceAccess.All,
                [TenantPermission.ConcertsManage] = ResourceAccess.All,
                [TenantPermission.ConcertsOpsEdit] = ResourceAccess.All,
                [TenantPermission.ConcertsPublish] = ResourceAccess.All,
                [TenantPermission.MembersInvite] = ResourceAccess.All,
                [TenantPermission.MembersManageRoles] = ResourceAccess.All,
                [TenantPermission.MembersRemove] = ResourceAccess.All,
                [TenantPermission.MessagesRead] = ResourceAccess.All,
                [TenantPermission.MessagesSend] = ResourceAccess.All,
                [TenantPermission.OperationsView] = ResourceAccess.All,
                [TenantPermission.OpportunitiesManage] = ResourceAccess.All,
                [TenantPermission.PayoutsManage] = ResourceAccess.All,
                [TenantPermission.ProfileEdit] = ResourceAccess.All,
                [TenantPermission.ResourcesShare] = ResourceAccess.All,
                [TenantPermission.SettlementTrigger] = ResourceAccess.All,
                [TenantPermission.SettlementView] = ResourceAccess.All,
                [TenantPermission.TenantDelete] = ResourceAccess.All,
                [TenantPermission.TenantSettingsEdit] = ResourceAccess.All,
                [TenantPermission.TermsRead] = ResourceAccess.All,
            }.ToFrozenDictionary(),
            [SystemRole.Manager] = new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.ApplicationsDecide] = ResourceAccess.All,
                [TenantPermission.ApplicationsSubmit] = ResourceAccess.All,
                [TenantPermission.BookingsCancel] = ResourceAccess.All,
                [TenantPermission.ConcertsCheckIn] = ResourceAccess.All,
                [TenantPermission.ConcertsDeclareDoorRevenue] = ResourceAccess.All,
                [TenantPermission.ConcertsManage] = ResourceAccess.All,
                [TenantPermission.ConcertsOpsEdit] = ResourceAccess.All,
                [TenantPermission.ConcertsPublish] = ResourceAccess.All,
                [TenantPermission.MembersInvite] = ResourceAccess.All,
                [TenantPermission.MessagesRead] = ResourceAccess.All,
                [TenantPermission.MessagesSend] = ResourceAccess.All,
                [TenantPermission.OperationsView] = ResourceAccess.All,
                [TenantPermission.OpportunitiesManage] = ResourceAccess.All,
                [TenantPermission.ProfileEdit] = ResourceAccess.All,
                [TenantPermission.ResourcesShare] = ResourceAccess.All,
                [TenantPermission.SettlementView] = ResourceAccess.All,
                [TenantPermission.TermsRead] = ResourceAccess.All,
            }.ToFrozenDictionary(),
            [SystemRole.Finance] = new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.MessagesRead] = ResourceAccess.All,
                [TenantPermission.OperationsView] = ResourceAccess.All,
                [TenantPermission.PayoutsManage] = ResourceAccess.All,
                [TenantPermission.SettlementTrigger] = ResourceAccess.All,
                [TenantPermission.SettlementView] = ResourceAccess.All,
                [TenantPermission.TermsRead] = ResourceAccess.All,
            }.ToFrozenDictionary(),
            [SystemRole.Staff] = new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.ConcertsCheckIn] = ResourceAccess.Assigned,
                [TenantPermission.ConcertsOpsEdit] = ResourceAccess.Assigned,
                [TenantPermission.MessagesRead] = ResourceAccess.Assigned,
                [TenantPermission.MessagesSend] = ResourceAccess.Assigned,
                [TenantPermission.OperationsView] = ResourceAccess.Assigned,
            }.ToFrozenDictionary(),
            [SystemRole.Door] = new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.ConcertsCheckIn] = ResourceAccess.Assigned,
                [TenantPermission.OperationsView] = ResourceAccess.Assigned,
            }.ToFrozenDictionary(),
            [SystemRole.Sound] = new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.ConcertsOpsEdit] = ResourceAccess.Assigned,
                [TenantPermission.OperationsView] = ResourceAccess.Assigned,
            }.ToFrozenDictionary(),
        }.ToFrozenDictionary();

    private static readonly FrozenSet<SystemRole> ProtectedRoles =
        new HashSet<SystemRole>
        {
            SystemRole.Owner,
        }.ToFrozenSet();

    private static readonly FrozenSet<SystemRole> InvitationAssignableRoles =
        new HashSet<SystemRole>
        {
            SystemRole.Staff,
            SystemRole.Door,
            SystemRole.Sound,
        }.ToFrozenSet();

    private static readonly FrozenDictionary<SystemRole, TenantBusinessActivityKind?> RequiredActivityByRole =
        new Dictionary<SystemRole, TenantBusinessActivityKind?>
        {
            [SystemRole.Owner] = null,
            [SystemRole.Manager] = null,
            [SystemRole.Finance] = null,
            [SystemRole.Staff] = null,
            [SystemRole.Door] = TenantBusinessActivityKind.VenueOperator,
            [SystemRole.Sound] = TenantBusinessActivityKind.VenueOperator,
        }.ToFrozenDictionary();

    extension(SystemRole role)
    {
        public IReadOnlyDictionary<TenantPermission, ResourceAccess> Grants => GrantsByRole[role];
        public bool IsProtected => ProtectedRoles.Contains(role);
        public bool IsInvitationAssignable => InvitationAssignableRoles.Contains(role);
        public TenantBusinessActivityKind? RequiredActivity => RequiredActivityByRole[role];

        public Guid IdFor(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Unknown tenant or system role.");
            var input = tenantId.ToByteArray().Concat(Encoding.UTF8.GetBytes(role.ToString())).ToArray();
            var hash = SHA256.HashData(input);
            return new Guid(hash.AsSpan(0, 16));
        }
    }
}

public static class SystemRoles
{
    public static Sha256Digest Fingerprint { get; } = Sha256Digest.From("8490b4ceb4b71eb7ed121b05a96ee1d6ab69eed02e30670d684f1a65e77f60b8");

    public static IReadOnlyList<SystemRole> All { get; } =
    [
        SystemRole.Owner,
        SystemRole.Manager,
        SystemRole.Finance,
        SystemRole.Staff,
        SystemRole.Door,
        SystemRole.Sound,
    ];

    public static IReadOnlyList<SystemRole> AvailableTo(IEnumerable<TenantBusinessActivityKind> activities)
    {
        var active = activities as ICollection<TenantBusinessActivityKind> ?? activities.ToArray();
        return [.. All.Where(role => role.RequiredActivity is not { } required || active.Contains(required))];
    }
}
