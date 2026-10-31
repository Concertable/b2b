#nullable enable
using System.Collections.Frozen;

namespace Concertable.B2B.Authorization.Contracts;

public enum AuthorizationResource
{
    Tenant = 1,
    Conversation,
    Concert,
    Opportunity,
    Application,
    Booking,
    Invoice,
    Contract,
}

public sealed record PermissionResourceBinding(
    AuthorizationResource Resource,
    string? Scope,
    ResourcePolicy Policy,
    IReadOnlyList<string> RequiresScopes);

public sealed record PermissionDescriptor(
    TenantPermission Permission,
    string Label,
    string Category,
    IReadOnlyList<PermissionResourceBinding> ResourceBindings,
    IReadOnlyList<ResourceAccess> AssignableAccess,
    bool OwnerOnly);

public sealed record SystemPresetDescriptor(
    string Key,
    bool IsProtectedOwner,
    bool IsInvitationAssignable,
    IReadOnlyDictionary<TenantPermission, ResourceAccess> Permissions);

public static class AuthorizationCatalog
{
    public const string Revision = "1af96819c91a46228e6d425e40a25610ffbdf840e1ae2d4d156287b718c9feb5";

    public static IReadOnlyDictionary<TenantPermission, PermissionDescriptor> Permissions { get; } =
        new Dictionary<TenantPermission, PermissionDescriptor>
        {
            [TenantPermission.ApplicationsDecide] = new(TenantPermission.ApplicationsDecide, "Decide applications", "Application", [new(AuthorizationResource.Application, "Proposal", ResourcePolicy.VenuePrincipal, ["Proposal"])], [ResourceAccess.All], false),
            [TenantPermission.ApplicationsSubmit] = new(TenantPermission.ApplicationsSubmit, "Submit applications", "Application", [new(AuthorizationResource.Application, "Proposal", ResourcePolicy.ArtistPrincipal, ["Proposal"])], [ResourceAccess.All], false),
            [TenantPermission.BookingsCancel] = new(TenantPermission.BookingsCancel, "Cancel bookings", "Booking", [new(AuthorizationResource.Booking, "Operations", ResourcePolicy.EitherPrincipal, ["Operations"])], [ResourceAccess.All], false),
            [TenantPermission.ConcertsCheckIn] = new(TenantPermission.ConcertsCheckIn, "Check in attendees", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceAccess.Assigned, ResourceAccess.All], false),
            [TenantPermission.ConcertsDeclareDoorRevenue] = new(TenantPermission.ConcertsDeclareDoorRevenue, "Declare door revenue", "Concert", [new(AuthorizationResource.Concert, "Finance", ResourcePolicy.VenuePrincipal, ["Operations", "Finance"])], [ResourceAccess.All], false),
            [TenantPermission.ConcertsManage] = new(TenantPermission.ConcertsManage, "Manage concerts", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.EitherPrincipal, ["Operations"])], [ResourceAccess.All], false),
            [TenantPermission.ConcertsOpsEdit] = new(TenantPermission.ConcertsOpsEdit, "Edit concert operations", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceAccess.Assigned, ResourceAccess.All], false),
            [TenantPermission.ConcertsPublish] = new(TenantPermission.ConcertsPublish, "Publish concerts", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceAccess.All], false),
            [TenantPermission.MembersInvite] = new(TenantPermission.MembersInvite, "Invite members", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.All], false),
            [TenantPermission.MembersManageRoles] = new(TenantPermission.MembersManageRoles, "Manage member roles", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.All], true),
            [TenantPermission.MembersRemove] = new(TenantPermission.MembersRemove, "Remove members", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.All], true),
            [TenantPermission.MessagesRead] = new(TenantPermission.MessagesRead, "Read messages", "Conversations", [new(AuthorizationResource.Conversation, "Read", ResourcePolicy.ConversationGrant, ["Read"])], [ResourceAccess.Assigned, ResourceAccess.All], false),
            [TenantPermission.MessagesSend] = new(TenantPermission.MessagesSend, "Send messages", "Conversations", [new(AuthorizationResource.Conversation, "SendMessages", ResourcePolicy.ConversationGrant, ["SendMessages"]), new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.Assigned, ResourceAccess.All], false),
            [TenantPermission.OperationsView] = new(TenantPermission.OperationsView, "View operations", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, []), new(AuthorizationResource.Application, "Summary", ResourcePolicy.ApplicationGrant, ["Summary"]), new(AuthorizationResource.Booking, "Summary", ResourcePolicy.BookingGrant, ["Summary"]), new(AuthorizationResource.Booking, "Operations", ResourcePolicy.BookingGrant, ["Operations"]), new(AuthorizationResource.Concert, "Summary", ResourcePolicy.ConcertGrant, ["Summary"]), new(AuthorizationResource.Concert, "Operations", ResourcePolicy.ConcertGrant, ["Operations"])], [ResourceAccess.Assigned, ResourceAccess.All], false),
            [TenantPermission.OpportunitiesManage] = new(TenantPermission.OpportunitiesManage, "Manage opportunities", "Opportunity", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.All], false),
            [TenantPermission.PayoutsManage] = new(TenantPermission.PayoutsManage, "Manage payouts", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.All], false),
            [TenantPermission.ProfileEdit] = new(TenantPermission.ProfileEdit, "Edit business profiles", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.All], false),
            [TenantPermission.ResourcesShare] = new(TenantPermission.ResourcesShare, "Share resources", "Concert", [new(AuthorizationResource.Concert, null, ResourcePolicy.PrincipalAdministration, []), new(AuthorizationResource.Conversation, null, ResourcePolicy.PrincipalAdministration, [])], [ResourceAccess.All], false),
            [TenantPermission.SettlementTrigger] = new(TenantPermission.SettlementTrigger, "Trigger settlement", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.All], false),
            [TenantPermission.SettlementView] = new(TenantPermission.SettlementView, "View settlement", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, []), new(AuthorizationResource.Concert, "Finance", ResourcePolicy.ConcertGrant, ["Finance"]), new(AuthorizationResource.Invoice, "Read", ResourcePolicy.InvoiceGrant, ["Read"])], [ResourceAccess.All], false),
            [TenantPermission.TenantDelete] = new(TenantPermission.TenantDelete, "Delete organization", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.All], true),
            [TenantPermission.TenantSettingsEdit] = new(TenantPermission.TenantSettingsEdit, "Edit organization settings", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAccess.All], false),
            [TenantPermission.TermsRead] = new(TenantPermission.TermsRead, "Read agreement terms", "Booking", [new(AuthorizationResource.Application, "Proposal", ResourcePolicy.ApplicationGrant, ["Proposal"]), new(AuthorizationResource.Contract, "Read", ResourcePolicy.ContractGrant, ["Read"])], [ResourceAccess.All], false),
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
