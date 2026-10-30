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
            [TenantPermission.ApplicationsDecide] = new(TenantPermission.ApplicationsDecide, "Decide applications", "Application", [new(AuthorizationResource.Application, "Proposal", ResourcePolicy.VenuePrincipal, ["Proposal"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ApplicationsSubmit] = new(TenantPermission.ApplicationsSubmit, "Submit applications", "Application", [new(AuthorizationResource.Application, "Proposal", ResourcePolicy.ArtistPrincipal, ["Proposal"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.BookingsCancel] = new(TenantPermission.BookingsCancel, "Cancel bookings", "Booking", [new(AuthorizationResource.Booking, "Operations", ResourcePolicy.EitherPrincipal, ["Operations"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsCheckIn] = new(TenantPermission.ConcertsCheckIn, "Check in attendees", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsDeclareDoorRevenue] = new(TenantPermission.ConcertsDeclareDoorRevenue, "Declare door revenue", "Concert", [new(AuthorizationResource.Concert, "Finance", ResourcePolicy.VenuePrincipal, ["Operations", "Finance"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsManage] = new(TenantPermission.ConcertsManage, "Manage concerts", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.EitherPrincipal, ["Operations"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsOpsEdit] = new(TenantPermission.ConcertsOpsEdit, "Edit concert operations", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.ConcertsPublish] = new(TenantPermission.ConcertsPublish, "Publish concerts", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.MembersInvite] = new(TenantPermission.MembersInvite, "Invite members", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.MembersManageRoles] = new(TenantPermission.MembersManageRoles, "Manage member roles", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], true),
            [TenantPermission.MembersRemove] = new(TenantPermission.MembersRemove, "Remove members", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], true),
            [TenantPermission.MessagesRead] = new(TenantPermission.MessagesRead, "Read messages", "Conversations", [new(AuthorizationResource.Conversation, "Read", ResourcePolicy.ConversationGrant, ["Read"])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.MessagesSend] = new(TenantPermission.MessagesSend, "Send messages", "Conversations", [new(AuthorizationResource.Conversation, "SendMessages", ResourcePolicy.ConversationGrant, ["SendMessages"]), new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.OperationsView] = new(TenantPermission.OperationsView, "View operations", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, []), new(AuthorizationResource.Application, "Summary", ResourcePolicy.ApplicationGrant, ["Summary"]), new(AuthorizationResource.Booking, "Summary", ResourcePolicy.BookingGrant, ["Summary"]), new(AuthorizationResource.Booking, "Operations", ResourcePolicy.BookingGrant, ["Operations"]), new(AuthorizationResource.Concert, "Summary", ResourcePolicy.ConcertGrant, ["Summary"]), new(AuthorizationResource.Concert, "Operations", ResourcePolicy.ConcertGrant, ["Operations"])], [ResourceAudience.AssignedResources, ResourceAudience.TenantResources], false),
            [TenantPermission.OpportunitiesManage] = new(TenantPermission.OpportunitiesManage, "Manage opportunities", "Opportunity", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.PayoutsManage] = new(TenantPermission.PayoutsManage, "Manage payouts", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ProfileEdit] = new(TenantPermission.ProfileEdit, "Edit business profiles", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.ResourcesShare] = new(TenantPermission.ResourcesShare, "Share resources", "Concert", [new(AuthorizationResource.Concert, null, ResourcePolicy.PrincipalAdministration, []), new(AuthorizationResource.Conversation, null, ResourcePolicy.PrincipalAdministration, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.SettlementTrigger] = new(TenantPermission.SettlementTrigger, "Trigger settlement", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.SettlementView] = new(TenantPermission.SettlementView, "View settlement", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, []), new(AuthorizationResource.Concert, "Finance", ResourcePolicy.ConcertGrant, ["Finance"]), new(AuthorizationResource.Invoice, "Read", ResourcePolicy.InvoiceGrant, ["Read"])], [ResourceAudience.TenantResources], false),
            [TenantPermission.TenantDelete] = new(TenantPermission.TenantDelete, "Delete organization", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], true),
            [TenantPermission.TenantSettingsEdit] = new(TenantPermission.TenantSettingsEdit, "Edit organization settings", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceAudience.TenantResources], false),
            [TenantPermission.TermsRead] = new(TenantPermission.TermsRead, "Read agreement terms", "Booking", [new(AuthorizationResource.Application, "Proposal", ResourcePolicy.ApplicationGrant, ["Proposal"]), new(AuthorizationResource.Contract, "Read", ResourcePolicy.ContractGrant, ["Read"])], [ResourceAudience.TenantResources], false),
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
