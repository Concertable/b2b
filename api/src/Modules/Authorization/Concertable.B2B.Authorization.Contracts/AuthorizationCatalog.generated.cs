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
    IReadOnlyList<ResourceCoverage> AssignableCoverages,
    bool OwnerOnly);

public sealed record SystemPresetDescriptor(
    string Key,
    bool IsProtectedOwner,
    bool IsInvitationAssignable,
    IReadOnlyDictionary<TenantPermission, ResourceCoverage> Permissions);

public static class AuthorizationCatalog
{
    public const string Revision = "8b2f220b198af94ea7592c8d895be0dabaf24561bc53fdade387e08c18f30dde";

    public static IReadOnlyDictionary<TenantPermission, PermissionDescriptor> Permissions { get; } =
        new Dictionary<TenantPermission, PermissionDescriptor>
        {
            [TenantPermission.ApplicationsDecide] = new(TenantPermission.ApplicationsDecide, "Decide applications", "Application", [new(AuthorizationResource.Application, "Proposal", ResourcePolicy.VenuePrincipal, ["Proposal"])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.ApplicationsSubmit] = new(TenantPermission.ApplicationsSubmit, "Submit applications", "Application", [new(AuthorizationResource.Application, "Proposal", ResourcePolicy.ArtistPrincipal, ["Proposal"])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.BookingsCancel] = new(TenantPermission.BookingsCancel, "Cancel bookings", "Booking", [new(AuthorizationResource.Booking, "Operations", ResourcePolicy.EitherPrincipal, ["Operations"])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.ConcertsCheckIn] = new(TenantPermission.ConcertsCheckIn, "Check in attendees", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceCoverage.AssignedResources, ResourceCoverage.TenantResources], false),
            [TenantPermission.ConcertsDeclareDoorRevenue] = new(TenantPermission.ConcertsDeclareDoorRevenue, "Declare door revenue", "Concert", [new(AuthorizationResource.Concert, "Finance", ResourcePolicy.VenuePrincipal, ["Operations", "Finance"])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.ConcertsManage] = new(TenantPermission.ConcertsManage, "Manage concerts", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.EitherPrincipal, ["Operations"])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.ConcertsOpsEdit] = new(TenantPermission.ConcertsOpsEdit, "Edit concert operations", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceCoverage.AssignedResources, ResourceCoverage.TenantResources], false),
            [TenantPermission.ConcertsPublish] = new(TenantPermission.ConcertsPublish, "Publish concerts", "Concert", [new(AuthorizationResource.Concert, "Operations", ResourcePolicy.VenuePrincipal, ["Operations"])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.MembersInvite] = new(TenantPermission.MembersInvite, "Invite members", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.MembersManageRoles] = new(TenantPermission.MembersManageRoles, "Manage member roles", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.TenantResources], true),
            [TenantPermission.MembersRemove] = new(TenantPermission.MembersRemove, "Remove members", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.TenantResources], true),
            [TenantPermission.MessagesRead] = new(TenantPermission.MessagesRead, "Read messages", "Conversations", [new(AuthorizationResource.Conversation, "Read", ResourcePolicy.ConversationGrant, ["Read"])], [ResourceCoverage.AssignedResources, ResourceCoverage.TenantResources], false),
            [TenantPermission.MessagesSend] = new(TenantPermission.MessagesSend, "Send messages", "Conversations", [new(AuthorizationResource.Conversation, "SendMessages", ResourcePolicy.ConversationGrant, ["SendMessages"]), new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.AssignedResources, ResourceCoverage.TenantResources], false),
            [TenantPermission.OperationsView] = new(TenantPermission.OperationsView, "View operations", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, []), new(AuthorizationResource.Application, "Summary", ResourcePolicy.ApplicationGrant, ["Summary"]), new(AuthorizationResource.Booking, "Summary", ResourcePolicy.BookingGrant, ["Summary"]), new(AuthorizationResource.Booking, "Operations", ResourcePolicy.BookingGrant, ["Operations"]), new(AuthorizationResource.Concert, "Summary", ResourcePolicy.ConcertGrant, ["Summary"]), new(AuthorizationResource.Concert, "Operations", ResourcePolicy.ConcertGrant, ["Operations"])], [ResourceCoverage.AssignedResources, ResourceCoverage.TenantResources], false),
            [TenantPermission.OpportunitiesManage] = new(TenantPermission.OpportunitiesManage, "Manage opportunities", "Opportunity", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.PayoutsManage] = new(TenantPermission.PayoutsManage, "Manage payouts", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.ProfileEdit] = new(TenantPermission.ProfileEdit, "Edit business profiles", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.ResourcesShare] = new(TenantPermission.ResourcesShare, "Share resources", "Concert", [new(AuthorizationResource.Concert, null, ResourcePolicy.PrincipalAdministration, []), new(AuthorizationResource.Conversation, null, ResourcePolicy.PrincipalAdministration, [])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.SettlementTrigger] = new(TenantPermission.SettlementTrigger, "Trigger settlement", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.SettlementView] = new(TenantPermission.SettlementView, "View settlement", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, []), new(AuthorizationResource.Concert, "Finance", ResourcePolicy.ConcertGrant, ["Finance"]), new(AuthorizationResource.Invoice, "Read", ResourcePolicy.InvoiceGrant, ["Read"])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.TenantDelete] = new(TenantPermission.TenantDelete, "Delete organization", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.TenantResources], true),
            [TenantPermission.TenantSettingsEdit] = new(TenantPermission.TenantSettingsEdit, "Edit organization settings", "Tenant", [new(AuthorizationResource.Tenant, null, ResourcePolicy.Membership, [])], [ResourceCoverage.TenantResources], false),
            [TenantPermission.TermsRead] = new(TenantPermission.TermsRead, "Read agreement terms", "Booking", [new(AuthorizationResource.Application, "Proposal", ResourcePolicy.ApplicationGrant, ["Proposal"]), new(AuthorizationResource.Contract, "Read", ResourcePolicy.ContractGrant, ["Read"])], [ResourceCoverage.TenantResources], false),
        }.ToFrozenDictionary();

    public static IReadOnlyDictionary<string, SystemPresetDescriptor> Presets { get; } =
        new Dictionary<string, SystemPresetDescriptor>(StringComparer.Ordinal)
        {
            ["Owner"] = new("Owner", true, false, new Dictionary<TenantPermission, ResourceCoverage>
            {
                [TenantPermission.ApplicationsDecide] = ResourceCoverage.TenantResources,
                [TenantPermission.ApplicationsSubmit] = ResourceCoverage.TenantResources,
                [TenantPermission.BookingsCancel] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsCheckIn] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsDeclareDoorRevenue] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsManage] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsOpsEdit] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsPublish] = ResourceCoverage.TenantResources,
                [TenantPermission.MembersInvite] = ResourceCoverage.TenantResources,
                [TenantPermission.MembersManageRoles] = ResourceCoverage.TenantResources,
                [TenantPermission.MembersRemove] = ResourceCoverage.TenantResources,
                [TenantPermission.MessagesRead] = ResourceCoverage.TenantResources,
                [TenantPermission.MessagesSend] = ResourceCoverage.TenantResources,
                [TenantPermission.OperationsView] = ResourceCoverage.TenantResources,
                [TenantPermission.OpportunitiesManage] = ResourceCoverage.TenantResources,
                [TenantPermission.PayoutsManage] = ResourceCoverage.TenantResources,
                [TenantPermission.ProfileEdit] = ResourceCoverage.TenantResources,
                [TenantPermission.ResourcesShare] = ResourceCoverage.TenantResources,
                [TenantPermission.SettlementTrigger] = ResourceCoverage.TenantResources,
                [TenantPermission.SettlementView] = ResourceCoverage.TenantResources,
                [TenantPermission.TenantDelete] = ResourceCoverage.TenantResources,
                [TenantPermission.TenantSettingsEdit] = ResourceCoverage.TenantResources,
                [TenantPermission.TermsRead] = ResourceCoverage.TenantResources,
            }.ToFrozenDictionary()),
            ["Manager"] = new("Manager", false, false, new Dictionary<TenantPermission, ResourceCoverage>
            {
                [TenantPermission.ApplicationsDecide] = ResourceCoverage.TenantResources,
                [TenantPermission.ApplicationsSubmit] = ResourceCoverage.TenantResources,
                [TenantPermission.BookingsCancel] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsCheckIn] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsDeclareDoorRevenue] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsManage] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsOpsEdit] = ResourceCoverage.TenantResources,
                [TenantPermission.ConcertsPublish] = ResourceCoverage.TenantResources,
                [TenantPermission.MembersInvite] = ResourceCoverage.TenantResources,
                [TenantPermission.MessagesRead] = ResourceCoverage.TenantResources,
                [TenantPermission.MessagesSend] = ResourceCoverage.TenantResources,
                [TenantPermission.OperationsView] = ResourceCoverage.TenantResources,
                [TenantPermission.OpportunitiesManage] = ResourceCoverage.TenantResources,
                [TenantPermission.ProfileEdit] = ResourceCoverage.TenantResources,
                [TenantPermission.ResourcesShare] = ResourceCoverage.TenantResources,
                [TenantPermission.SettlementView] = ResourceCoverage.TenantResources,
                [TenantPermission.TermsRead] = ResourceCoverage.TenantResources,
            }.ToFrozenDictionary()),
            ["Finance"] = new("Finance", false, false, new Dictionary<TenantPermission, ResourceCoverage>
            {
                [TenantPermission.MessagesRead] = ResourceCoverage.TenantResources,
                [TenantPermission.OperationsView] = ResourceCoverage.TenantResources,
                [TenantPermission.PayoutsManage] = ResourceCoverage.TenantResources,
                [TenantPermission.SettlementTrigger] = ResourceCoverage.TenantResources,
                [TenantPermission.SettlementView] = ResourceCoverage.TenantResources,
                [TenantPermission.TermsRead] = ResourceCoverage.TenantResources,
            }.ToFrozenDictionary()),
            ["Staff"] = new("Staff", false, true, new Dictionary<TenantPermission, ResourceCoverage>
            {
                [TenantPermission.ConcertsCheckIn] = ResourceCoverage.AssignedResources,
                [TenantPermission.ConcertsOpsEdit] = ResourceCoverage.AssignedResources,
                [TenantPermission.MessagesRead] = ResourceCoverage.AssignedResources,
                [TenantPermission.MessagesSend] = ResourceCoverage.AssignedResources,
                [TenantPermission.OperationsView] = ResourceCoverage.AssignedResources,
            }.ToFrozenDictionary()),
            ["Door"] = new("Door", false, true, new Dictionary<TenantPermission, ResourceCoverage>
            {
                [TenantPermission.ConcertsCheckIn] = ResourceCoverage.AssignedResources,
                [TenantPermission.OperationsView] = ResourceCoverage.AssignedResources,
            }.ToFrozenDictionary()),
            ["Sound"] = new("Sound", false, true, new Dictionary<TenantPermission, ResourceCoverage>
            {
                [TenantPermission.ConcertsOpsEdit] = ResourceCoverage.AssignedResources,
                [TenantPermission.OperationsView] = ResourceCoverage.AssignedResources,
            }.ToFrozenDictionary()),
        }.ToFrozenDictionary(StringComparer.Ordinal);
}
