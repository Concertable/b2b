#nullable enable
using System.Collections.Frozen;

namespace Concertable.B2B.Authorization.Contracts;

public sealed record PermissionResourceBinding(
    string Resource,
    string? Facet,
    ResourcePolicy Policy,
    IReadOnlyList<string> RequiresScopes);

public sealed record PermissionDescriptor(
    TenantPermission Permission,
    string Label,
    string Category,
    IReadOnlyList<PermissionResourceBinding> ResourceBindings,
    IReadOnlyList<ResourceAudience> AssignableAudiences,
    bool OwnerOnly);

public sealed record SystemPresetDescriptor(
    string Key,
    bool IsProtectedOwner,
    bool IsInvitationAssignable,
    IReadOnlyDictionary<TenantPermission, ResourceAudience> Permissions);

public static class AuthorizationCatalog
{
    public const string Revision = "8b2f220b198af94ea7592c8d895be0dabaf24561bc53fdade387e08c18f30dde";

    public static IReadOnlyDictionary<TenantPermission, PermissionDescriptor> Permissions { get; } =
        new Dictionary<TenantPermission, PermissionDescriptor>
        {
            [TenantPermission.ApplicationsDecide] = new(TenantPermission.ApplicationsDecide, "Decide applications", "Application", [new("application", "Proposal", ResourcePolicy.VenuePrincipal, ["Proposal"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ApplicationsSubmit] = new(TenantPermission.ApplicationsSubmit, "Submit applications", "Application", [new("application", "Proposal", ResourcePolicy.ArtistPrincipal, ["Proposal"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.BookingsCancel] = new(TenantPermission.BookingsCancel, "Cancel bookings", "Booking", [new("booking", "Operations", ResourcePolicy.EitherPrincipal, ["Operations"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsCheckIn] = new(TenantPermission.ConcertsCheckIn, "Check in attendees", "Concert", [new("concert", "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsDeclareDoorRevenue] = new(TenantPermission.ConcertsDeclareDoorRevenue, "Declare door revenue", "Concert", [new("concert", "Finance", ResourcePolicy.VenuePrincipal, ["Operations", "Finance"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsManage] = new(TenantPermission.ConcertsManage, "Manage concerts", "Concert", [new("concert", "Operations", ResourcePolicy.EitherPrincipal, ["Operations"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsOpsEdit] = new(TenantPermission.ConcertsOpsEdit, "Edit concert operations", "Concert", [new("concert", "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsPublish] = new(TenantPermission.ConcertsPublish, "Publish concerts", "Concert", [new("concert", "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.MembersInvite] = new(TenantPermission.MembersInvite, "Invite members", "Tenant", [new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.MembersManageRoles] = new(TenantPermission.MembersManageRoles, "Manage member roles", "Tenant", [new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], true),
            [TenantPermission.MembersRemove] = new(TenantPermission.MembersRemove, "Remove members", "Tenant", [new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], true),
            [TenantPermission.MessagesRead] = new(TenantPermission.MessagesRead, "Read messages", "Conversations", [new("conversation", "Read", ResourcePolicy.ConversationGrant, ["Read"])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.MessagesSend] = new(TenantPermission.MessagesSend, "Send messages", "Conversations", [new("conversation", "SendMessages", ResourcePolicy.ConversationGrant, ["SendMessages"]), new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.OperationsView] = new(TenantPermission.OperationsView, "View operations", "Tenant", [new("tenant", null, ResourcePolicy.Membership, []), new("application", "Summary", ResourcePolicy.ApplicationGrant, ["Summary"]), new("booking", "Summary", ResourcePolicy.BookingGrant, ["Summary"]), new("booking", "Operations", ResourcePolicy.BookingGrant, ["Operations"]), new("concert", "Summary", ResourcePolicy.ConcertGrant, ["Summary"]), new("concert", "Operations", ResourcePolicy.ConcertGrant, ["Operations"])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.OpportunitiesManage] = new(TenantPermission.OpportunitiesManage, "Manage opportunities", "Opportunity", [new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.PayoutsManage] = new(TenantPermission.PayoutsManage, "Manage payouts", "Tenant", [new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ProfileEdit] = new(TenantPermission.ProfileEdit, "Edit business profiles", "Tenant", [new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ResourcesShare] = new(TenantPermission.ResourcesShare, "Share resources", "Concert", [new("concert", null, ResourcePolicy.PrincipalAdministration, []), new("conversation", null, ResourcePolicy.PrincipalAdministration, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.SettlementTrigger] = new(TenantPermission.SettlementTrigger, "Trigger settlement", "Tenant", [new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.SettlementView] = new(TenantPermission.SettlementView, "View settlement", "Tenant", [new("tenant", null, ResourcePolicy.Membership, []), new("concert", "Finance", ResourcePolicy.ConcertGrant, ["Finance"]), new("invoice", "Read", ResourcePolicy.InvoiceGrant, ["Read"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.TenantDelete] = new(TenantPermission.TenantDelete, "Delete organization", "Tenant", [new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], true),
            [TenantPermission.TenantSettingsEdit] = new(TenantPermission.TenantSettingsEdit, "Edit organization settings", "Tenant", [new("tenant", null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.TermsRead] = new(TenantPermission.TermsRead, "Read agreement terms", "Booking", [new("application", "Proposal", ResourcePolicy.ApplicationGrant, ["Proposal"]), new("contract", "Read", ResourcePolicy.ContractGrant, ["Read"])], [ResourceAudience.TenantResources], false),
        }.ToFrozenDictionary();

    public static IReadOnlyDictionary<string, SystemPresetDescriptor> Presets { get; } =
        new Dictionary<string, SystemPresetDescriptor>(StringComparer.Ordinal)
        {
            ["Owner"] = new("Owner", true, false, new Dictionary<TenantPermission, ResourceAudience>
            {
                [TenantPermission.ApplicationsDecide] = ResourceAudience.TenantResources,
                [TenantPermission.ApplicationsSubmit] = ResourceAudience.TenantResources,
                [TenantPermission.BookingsCancel] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsCheckIn] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsDeclareDoorRevenue] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsManage] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsOpsEdit] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsPublish] = ResourceAudience.TenantResources,
                [TenantPermission.MembersInvite] = ResourceAudience.TenantResources,
                [TenantPermission.MembersManageRoles] = ResourceAudience.TenantResources,
                [TenantPermission.MembersRemove] = ResourceAudience.TenantResources,
                [TenantPermission.MessagesRead] = ResourceAudience.TenantResources,
                [TenantPermission.MessagesSend] = ResourceAudience.TenantResources,
                [TenantPermission.OperationsView] = ResourceAudience.TenantResources,
                [TenantPermission.OpportunitiesManage] = ResourceAudience.TenantResources,
                [TenantPermission.PayoutsManage] = ResourceAudience.TenantResources,
                [TenantPermission.ProfileEdit] = ResourceAudience.TenantResources,
                [TenantPermission.ResourcesShare] = ResourceAudience.TenantResources,
                [TenantPermission.SettlementTrigger] = ResourceAudience.TenantResources,
                [TenantPermission.SettlementView] = ResourceAudience.TenantResources,
                [TenantPermission.TenantDelete] = ResourceAudience.TenantResources,
                [TenantPermission.TenantSettingsEdit] = ResourceAudience.TenantResources,
                [TenantPermission.TermsRead] = ResourceAudience.TenantResources,
            }.ToFrozenDictionary()),
            ["Manager"] = new("Manager", false, false, new Dictionary<TenantPermission, ResourceAudience>
            {
                [TenantPermission.ApplicationsDecide] = ResourceAudience.TenantResources,
                [TenantPermission.ApplicationsSubmit] = ResourceAudience.TenantResources,
                [TenantPermission.BookingsCancel] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsCheckIn] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsDeclareDoorRevenue] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsManage] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsOpsEdit] = ResourceAudience.TenantResources,
                [TenantPermission.ConcertsPublish] = ResourceAudience.TenantResources,
                [TenantPermission.MembersInvite] = ResourceAudience.TenantResources,
                [TenantPermission.MessagesRead] = ResourceAudience.TenantResources,
                [TenantPermission.MessagesSend] = ResourceAudience.TenantResources,
                [TenantPermission.OperationsView] = ResourceAudience.TenantResources,
                [TenantPermission.OpportunitiesManage] = ResourceAudience.TenantResources,
                [TenantPermission.ProfileEdit] = ResourceAudience.TenantResources,
                [TenantPermission.ResourcesShare] = ResourceAudience.TenantResources,
                [TenantPermission.SettlementView] = ResourceAudience.TenantResources,
                [TenantPermission.TermsRead] = ResourceAudience.TenantResources,
            }.ToFrozenDictionary()),
            ["Finance"] = new("Finance", false, false, new Dictionary<TenantPermission, ResourceAudience>
            {
                [TenantPermission.MessagesRead] = ResourceAudience.TenantResources,
                [TenantPermission.OperationsView] = ResourceAudience.TenantResources,
                [TenantPermission.PayoutsManage] = ResourceAudience.TenantResources,
                [TenantPermission.SettlementTrigger] = ResourceAudience.TenantResources,
                [TenantPermission.SettlementView] = ResourceAudience.TenantResources,
                [TenantPermission.TermsRead] = ResourceAudience.TenantResources,
            }.ToFrozenDictionary()),
            ["Staff"] = new("Staff", false, true, new Dictionary<TenantPermission, ResourceAudience>
            {
                [TenantPermission.ConcertsCheckIn] = ResourceAudience.AssignedResources,
                [TenantPermission.ConcertsOpsEdit] = ResourceAudience.AssignedResources,
                [TenantPermission.MessagesRead] = ResourceAudience.AssignedResources,
                [TenantPermission.MessagesSend] = ResourceAudience.AssignedResources,
                [TenantPermission.OperationsView] = ResourceAudience.AssignedResources,
            }.ToFrozenDictionary()),
            ["Door"] = new("Door", false, true, new Dictionary<TenantPermission, ResourceAudience>
            {
                [TenantPermission.ConcertsCheckIn] = ResourceAudience.AssignedResources,
                [TenantPermission.OperationsView] = ResourceAudience.AssignedResources,
            }.ToFrozenDictionary()),
            ["Sound"] = new("Sound", false, true, new Dictionary<TenantPermission, ResourceAudience>
            {
                [TenantPermission.ConcertsOpsEdit] = ResourceAudience.AssignedResources,
                [TenantPermission.OperationsView] = ResourceAudience.AssignedResources,
            }.ToFrozenDictionary()),
        }.ToFrozenDictionary(StringComparer.Ordinal);
}
