#nullable enable
using System.Collections.Frozen;

namespace Concertable.B2B.Authorization.Contracts;

public sealed record PermissionDescriptor(
    TenantPermission Permission,
    string Label,
    string Category,
    bool OwnerOnly);

public sealed record SystemPresetDescriptor(
    string Key,
    bool IsProtectedOwner,
    bool IsInvitationAssignable,
    IReadOnlyDictionary<TenantPermission, ResourceAccess> Permissions);

public static class AuthorizationCatalog
{
    public const string Revision = "43b845fd702a1b077afa0a3f23cab33d3bcdb8a7351ed53e9ea921cd9ff07b35";

    public static IReadOnlyDictionary<TenantPermission, PermissionDescriptor> Permissions { get; } =
        new Dictionary<TenantPermission, PermissionDescriptor>
        {
            [TenantPermission.ApplicationsDecide] = new(TenantPermission.ApplicationsDecide, "Decide applications", "Application", false),
            [TenantPermission.ApplicationsSubmit] = new(TenantPermission.ApplicationsSubmit, "Submit applications", "Application", false),
            [TenantPermission.BookingsCancel] = new(TenantPermission.BookingsCancel, "Cancel bookings", "Booking", false),
            [TenantPermission.ConcertsCheckIn] = new(TenantPermission.ConcertsCheckIn, "Check in attendees", "Concert", false),
            [TenantPermission.ConcertsDeclareDoorRevenue] = new(TenantPermission.ConcertsDeclareDoorRevenue, "Declare door revenue", "Concert", false),
            [TenantPermission.ConcertsManage] = new(TenantPermission.ConcertsManage, "Manage concerts", "Concert", false),
            [TenantPermission.ConcertsOpsEdit] = new(TenantPermission.ConcertsOpsEdit, "Edit concert operations", "Concert", false),
            [TenantPermission.ConcertsPublish] = new(TenantPermission.ConcertsPublish, "Publish concerts", "Concert", false),
            [TenantPermission.MembersInvite] = new(TenantPermission.MembersInvite, "Invite members", "Tenant", false),
            [TenantPermission.MembersManageRoles] = new(TenantPermission.MembersManageRoles, "Manage member roles", "Tenant", true),
            [TenantPermission.MembersRemove] = new(TenantPermission.MembersRemove, "Remove members", "Tenant", true),
            [TenantPermission.MessagesRead] = new(TenantPermission.MessagesRead, "Read messages", "Conversations", false),
            [TenantPermission.MessagesSend] = new(TenantPermission.MessagesSend, "Send messages", "Conversations", false),
            [TenantPermission.OperationsView] = new(TenantPermission.OperationsView, "View operations", "Tenant", false),
            [TenantPermission.OpportunitiesManage] = new(TenantPermission.OpportunitiesManage, "Manage opportunities", "Opportunity", false),
            [TenantPermission.PayoutsManage] = new(TenantPermission.PayoutsManage, "Manage payouts", "Tenant", false),
            [TenantPermission.ProfileEdit] = new(TenantPermission.ProfileEdit, "Edit business profiles", "Tenant", false),
            [TenantPermission.ResourcesShare] = new(TenantPermission.ResourcesShare, "Share resources", "Concert", false),
            [TenantPermission.SettlementTrigger] = new(TenantPermission.SettlementTrigger, "Trigger settlement", "Tenant", false),
            [TenantPermission.SettlementView] = new(TenantPermission.SettlementView, "View settlement", "Tenant", false),
            [TenantPermission.TenantDelete] = new(TenantPermission.TenantDelete, "Delete organization", "Tenant", true),
            [TenantPermission.TenantSettingsEdit] = new(TenantPermission.TenantSettingsEdit, "Edit organization settings", "Tenant", false),
            [TenantPermission.TermsRead] = new(TenantPermission.TermsRead, "Read agreement terms", "Booking", false),
        }.ToFrozenDictionary();

    public static IReadOnlyDictionary<string, SystemPresetDescriptor> Presets { get; } =
        new Dictionary<string, SystemPresetDescriptor>(StringComparer.Ordinal)
        {
            ["Owner"] = new("Owner", true, false, new Dictionary<TenantPermission, ResourceAccess>
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
            }.ToFrozenDictionary()),
            ["Manager"] = new("Manager", false, false, new Dictionary<TenantPermission, ResourceAccess>
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
            }.ToFrozenDictionary()),
            ["Finance"] = new("Finance", false, false, new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.MessagesRead] = ResourceAccess.All,
                [TenantPermission.OperationsView] = ResourceAccess.All,
                [TenantPermission.PayoutsManage] = ResourceAccess.All,
                [TenantPermission.SettlementTrigger] = ResourceAccess.All,
                [TenantPermission.SettlementView] = ResourceAccess.All,
                [TenantPermission.TermsRead] = ResourceAccess.All,
            }.ToFrozenDictionary()),
            ["Staff"] = new("Staff", false, true, new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.ConcertsCheckIn] = ResourceAccess.Assigned,
                [TenantPermission.ConcertsOpsEdit] = ResourceAccess.Assigned,
                [TenantPermission.MessagesRead] = ResourceAccess.Assigned,
                [TenantPermission.MessagesSend] = ResourceAccess.Assigned,
                [TenantPermission.OperationsView] = ResourceAccess.Assigned,
            }.ToFrozenDictionary()),
            ["Door"] = new("Door", false, true, new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.ConcertsCheckIn] = ResourceAccess.Assigned,
                [TenantPermission.OperationsView] = ResourceAccess.Assigned,
            }.ToFrozenDictionary()),
            ["Sound"] = new("Sound", false, true, new Dictionary<TenantPermission, ResourceAccess>
            {
                [TenantPermission.ConcertsOpsEdit] = ResourceAccess.Assigned,
                [TenantPermission.OperationsView] = ResourceAccess.Assigned,
            }.ToFrozenDictionary()),
        }.ToFrozenDictionary(StringComparer.Ordinal);
}
