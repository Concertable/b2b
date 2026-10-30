using Concertable.B2B.Tenant.Domain;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

internal sealed class TenantAuthorityResolver : IAuthorityResolver
{
    private readonly ITenantRepository tenants;
    private readonly IMembershipRepository memberships;
    private readonly IRoleRepository roles;
    private readonly IInvitationRepository invitations;
    private readonly IMembershipContext membershipContext;
    private readonly IAuthorizationContext authorizationContext;
    private readonly TimeProvider clock;
    private UnitOfWorkState? state;

    public TenantAuthorityResolver(
        ITenantRepository tenants,
        IMembershipRepository memberships,
        IRoleRepository roles,
        IInvitationRepository invitations,
        IMembershipContext membershipContext,
        IAuthorizationContext authorizationContext,
        TimeProvider clock)
    {
        this.tenants = tenants;
        this.memberships = memberships;
        this.roles = roles;
        this.invitations = invitations;
        this.membershipContext = membershipContext;
        this.authorizationContext = authorizationContext;
        this.clock = clock;
    }

    public async Task<Option<AuthoritySnapshot>> ResolveAsync(
        MembershipSnapshot requestActor, CancellationToken ct = default) =>
        (await memberships.GetAuthoritySnapshotByUserIdAndTenantIdAsync(
            requestActor.UserId, requestActor.TenantId, ct)).ToOption();

    public async Task<Option<AuthoritySnapshot>> ResolveForUnitOfWorkAsync(
        MembershipSnapshot requestActor, CancellationToken ct = default)
    {
        var current = StateForCurrentUnitOfWork();
        if (current.Original is { } retained)
            return retained.Actor.MembershipId == requestActor.MembershipId
                && retained.Actor.TenantId == requestActor.TenantId
                && retained.Actor.UserId == requestActor.UserId
                ? retained : null;

        if (!(await tenants.GetExistingIdsForShareAsync([requestActor.TenantId], ct))
            .Contains(requestActor.TenantId))
            return null;
        var actor = (await memberships.GetSnapshotsByIdsForShareAsync(
            [requestActor.MembershipId], ct)).SingleOrDefault();
        var revision = await tenants.GetAuthorizationCatalogRevisionAsync(ct);
        if (actor is null || !actor.HasSameAuthorityAs(requestActor)
            || revision != AuthorizationCatalog.Revision)
            return null;

        current.Original = new AuthoritySnapshot(actor, revision);
        current.ExpectedPolicyVersion = actor.RolePolicyVersion;
        return current.Original;
    }

    public async Task<bool> ValidateForCommitAsync(
        AuthoritySnapshot original, CancellationToken ct = default)
    {
        var current = ExistingState();
        return current?.Original is { } retained
            && retained.Actor.HasSameAuthorityAs(original.Actor)
            && retained.CatalogRevision == original.CatalogRevision
            && await ValidateStateAsync(current, ct);
    }

    internal async Task<MembershipSnapshot?> AuthorizeAdministrationAsync(
        Guid tenantId, TenantPermission permission, bool protectedOwner,
        CancellationToken ct = default)
    {
        var expected = membershipContext.Membership;
        if (expected is null || expected.TenantId != tenantId)
            return null;
        var resolved = await ResolveForUnitOfWorkAsync(expected, ct);
        if (!resolved.TryGetValue(out var authority)
            || !authority.Actor.HasPermission(permission))
            return null;
        var current = ExistingState()!;
        if (current.InitialIsProtectedOwner is null)
            current.InitialIsProtectedOwner = await roles.HasProtectedOwnerAsync(
                tenantId, authority.Actor.MembershipId, ct);
        if (protectedOwner && current.InitialIsProtectedOwner != true)
            return null;
        current.AuthorizedPermissions.Add(permission);
        return authority.Actor;
    }

    internal async Task<bool> TrackMembershipAsync(
        TenantMembershipEntity member, CancellationToken ct = default)
    {
        var current = AuthorizedState();
        if (current is null || member.TenantId != current.Original!.Actor.TenantId)
            return false;
        if (current.Members.TryGetValue(member.Id, out var tracked))
            return tracked.TenantId == member.TenantId
                && tracked.UserId == member.UserId
                && tracked.ExpectedVersion == member.PermissionVersion
                && !tracked.Removed;
        var snapshot = (await memberships.GetSnapshotsByIdsForUpdateAsync([member.Id], ct))
            .SingleOrDefault();
        if (snapshot is null || snapshot.TenantId != member.TenantId
            || snapshot.UserId != member.UserId
            || snapshot.PermissionVersion != member.PermissionVersion)
            return false;
        current.Members.Add(member.Id, new MemberTransition(
            member.TenantId, member.UserId, member.PermissionVersion));
        return true;
    }

    internal void RecordMembershipVersion(TenantMembershipEntity member, long beforeVersion)
    {
        var current = AuthorizedState() ?? throw new InvalidOperationException("No checked administration snapshot.");
        if (!current.Members.TryGetValue(member.Id, out var tracked)
            || tracked.Removed
            || tracked.TenantId != member.TenantId
            || tracked.UserId != member.UserId
            || tracked.ExpectedVersion != beforeVersion
            || member.PermissionVersion != beforeVersion + 1
            || !CanChangeMembership(current))
            throw new InvalidOperationException("Invalid membership authority transition.");
        tracked.ExpectedVersion = member.PermissionVersion;
        current.MembershipChanged = true;
    }

    internal void RecordMembershipRemoval(TenantMembershipEntity member)
    {
        var current = AuthorizedState() ?? throw new InvalidOperationException("No checked administration snapshot.");
        if (!current.Members.TryGetValue(member.Id, out var tracked)
            || tracked.Removed
            || tracked.TenantId != member.TenantId
            || tracked.UserId != member.UserId
            || tracked.ExpectedVersion != member.PermissionVersion
            || !CanChangeMembership(current))
            throw new InvalidOperationException("Invalid membership removal transition.");
        tracked.Removed = true;
        current.MembershipChanged = true;
    }

    internal void RecordPolicyVersion(TenantEntity tenant, long beforeVersion)
    {
        var current = AuthorizedState() ?? throw new InvalidOperationException("No checked administration snapshot.");
        if (tenant.Id != current.Original!.Actor.TenantId
            || !current.AuthorizedPermissions.Contains(TenantPermission.MembersManageRoles)
            || current.InitialIsProtectedOwner != true
            || current.ExpectedPolicyVersion != beforeVersion
            || tenant.RolePolicyVersion != beforeVersion + 1
            || current.TenantDeleted)
            throw new InvalidOperationException("Invalid role-policy transition.");
        current.ExpectedPolicyVersion = tenant.RolePolicyVersion;
    }

    internal async Task<bool> TrackTenantDeletionAsync(
        TenantEntity tenant, IReadOnlyCollection<TenantMembershipEntity> members,
        CancellationToken ct = default)
    {
        var current = AuthorizedState();
        if (current is null || tenant.Id != current.Original!.Actor.TenantId
            || !current.AuthorizedPermissions.Contains(TenantPermission.TenantDelete)
            || current.InitialIsProtectedOwner != true
            || current.ExpectedPolicyVersion != tenant.RolePolicyVersion
            || current.TenantDeleted)
            return false;
        foreach (var member in members.OrderBy(member => member.Id))
        {
            if (!await TrackMembershipAsync(member, ct))
                return false;
        }
        foreach (var member in members)
            RecordMembershipRemoval(member);
        current.TenantDeleted = true;
        return true;
    }

    internal bool AuthorizeInvitationCreation(
        TenantInvitationEntity invitation,
        MembershipSnapshot inviter,
        IReadOnlyCollection<TenantRoleDefinition> selectedRoles,
        CancellationToken ct = default)
    {
        var current = AuthorizedState();
        if (current?.Original is not { } original
            || !original.Actor.HasSameAuthorityAs(inviter)
            || !current.AuthorizedPermissions.Contains(TenantPermission.MembersInvite)
            || invitation.TenantId != inviter.TenantId
            || invitation.InviterMembershipId != inviter.MembershipId
            || !invitation.IsActive(clock.GetUtcNow().UtcDateTime)
            || !selectedRoles.Select(role => role.Id).ToHashSet().SetEquals(
                invitation.Assignments.Select(row => row.RoleId)))
            return false;
        var inviterOwner = current.InitialIsProtectedOwner == true;
        if (!TenantRoleAssignmentPolicy.CanAssign(inviter, inviterOwner, selectedRoles))
            return false;
        current.CreatedInvitations.Add(new InvitationCreationSnapshot(
            invitation.Id, invitation.TenantId, inviter,
            selectedRoles.Select(role => role.Id).Order().ToArray(),
            invitation.ExpiresAt, original.CatalogRevision));
        return true;
    }

    internal async Task<bool> AuthorizeInvitationAcceptanceAsync(
        TenantInvitationEntity invitation,
        MembershipSnapshot inviter,
        IReadOnlyCollection<TenantRoleDefinition> selectedRoles,
        Guid acceptingUserId,
        CancellationToken ct = default)
    {
        var current = StateForCurrentUnitOfWork();
        if (invitation.TenantId != inviter.TenantId
            || invitation.InviterMembershipId != inviter.MembershipId
            || invitation.InviterPermissionVersion != inviter.PermissionVersion
            || invitation.InviterRolePolicyVersion != inviter.RolePolicyVersion
            || !invitation.IsActive(clock.GetUtcNow().UtcDateTime)
            || selectedRoles.Count != invitation.Assignments.Count
            || !selectedRoles.Select(role => role.Id).ToHashSet().SetEquals(
                invitation.Assignments.Select(row => row.RoleId)))
            return false;
        var revision = await tenants.GetAuthorizationCatalogRevisionAsync(ct);
        if (revision != AuthorizationCatalog.Revision)
            return false;
        var inviterOwner = await roles.HasProtectedOwnerAsync(inviter.TenantId, inviter.MembershipId, ct);
        if (!TenantRoleAssignmentPolicy.CanAssign(inviter, inviterOwner, selectedRoles))
            return false;
        current.AcceptedInvitations.Add(new InvitationAcceptanceSnapshot(
            invitation.Id, invitation.TenantId, invitation.Version, acceptingUserId,
            inviter, selectedRoles.Select(role => role.Id).Order().ToArray(),
            invitation.ExpiresAt, revision));
        return true;
    }

    private UnitOfWorkState StateForCurrentUnitOfWork()
    {
        if (!authorizationContext.IsActive || authorizationContext.UnitOfWorkId is not { } unitOfWorkId)
            throw new InvalidOperationException("Authority requires an active unit of work.");
        if (state is not null && state.UnitOfWorkId == unitOfWorkId)
            return state;
        state = new UnitOfWorkState(unitOfWorkId);
        var captured = state;
        authorizationContext.RegisterValidator(ct => ValidateStateAsync(captured, ct));
        return state;
    }

    private UnitOfWorkState? ExistingState() =>
        authorizationContext.IsActive && authorizationContext.UnitOfWorkId is { } id && state?.UnitOfWorkId == id
            ? state : null;

    private UnitOfWorkState? AuthorizedState() =>
        ExistingState() is { Original: not null } current && current.AuthorizedPermissions.Count > 0
            ? current : null;

    private static bool CanChangeMembership(UnitOfWorkState current) =>
        current.InitialIsProtectedOwner == true
        && (current.AuthorizedPermissions.Contains(TenantPermission.MembersManageRoles)
            || current.AuthorizedPermissions.Contains(TenantPermission.MembersRemove)
            || current.AuthorizedPermissions.Contains(TenantPermission.TenantDelete));

    private async Task<bool> ValidateStateAsync(UnitOfWorkState current, CancellationToken ct)
    {
        if (!authorizationContext.IsActive || authorizationContext.UnitOfWorkId != current.UnitOfWorkId)
            return false;
        var revision = await tenants.GetAuthorizationCatalogRevisionAsync(ct);
        if (revision != AuthorizationCatalog.Revision)
            return false;
        if (current.Original is { } original)
        {
            if (original.CatalogRevision != revision)
                return false;
            var tenantId = original.Actor.TenantId;
            var exists = (await tenants.GetExistingIdsForShareAsync([tenantId], ct)).Contains(tenantId);
            if (current.TenantDeleted)
            {
                if (exists || (await memberships.GetSnapshotsByTenantIdsAsync([tenantId], ct)).Count != 0)
                    return false;
            }
            else
            {
                var tenant = await tenants.GetByIdAsync(tenantId, ct);
                if (!exists || tenant?.RolePolicyVersion != current.ExpectedPolicyVersion)
                    return false;
                if (current.MembershipChanged && await memberships.CountOwnersAsync(tenantId, ct) == 0)
                    return false;
            }
            foreach (var (membershipId, tracked) in current.Members)
            {
                var latest = await memberships.GetSnapshotByMembershipIdAsync(membershipId, ct);
                if (tracked.Removed)
                {
                    if (latest is not null
                        || await memberships.IsMemberAsync(tracked.TenantId, tracked.UserId, ct))
                        return false;
                }
                else if (latest is null || latest.TenantId != tracked.TenantId
                         || latest.UserId != tracked.UserId
                         || latest.PermissionVersion != tracked.ExpectedVersion)
                    return false;
            }
            var actor = await memberships.GetSnapshotByMembershipIdAsync(original.Actor.MembershipId, ct);
            if (!current.TenantDeleted
                && !current.Members.ContainsKey(original.Actor.MembershipId)
                && (actor is null || actor.MembershipId != original.Actor.MembershipId
                    || actor.TenantId != original.Actor.TenantId
                    || actor.UserId != original.Actor.UserId
                    || actor.PermissionVersion != original.Actor.PermissionVersion))
                return false;
            if (!current.TenantDeleted && current.ExpectedPolicyVersion == original.Actor.RolePolicyVersion
                && !current.Members.ContainsKey(original.Actor.MembershipId)
                && !actor!.HasSameAuthorityAs(original.Actor))
                return false;
        }
        foreach (var snapshot in current.CreatedInvitations)
        {
            if (snapshot.CatalogRevision != revision
                || clock.GetUtcNow().UtcDateTime >= snapshot.ExpiresAt)
                return false;
            var invitation = await invitations.GetByIdAsync(snapshot.InvitationId, ct);
            if (invitation is null || invitation.TenantId != snapshot.TenantId
                || invitation.Version != 1
                || invitation.Status != InvitationStatus.Pending
                || invitation.InviterMembershipId != snapshot.Inviter.MembershipId
                || !invitation.Assignments.Select(row => row.RoleId).Order().SequenceEqual(snapshot.RoleIds))
                return false;
            var inviter = await memberships.GetSnapshotByMembershipIdAsync(snapshot.Inviter.MembershipId, ct);
            if (inviter is null || !inviter.HasSameAuthorityAs(snapshot.Inviter))
                return false;
            var selected = await roles.ResolveActiveAsync(snapshot.TenantId, snapshot.RoleIds, ct);
            if (selected is null || !TenantRoleAssignmentPolicy.CanAssign(inviter,
                await roles.HasProtectedOwnerAsync(inviter.TenantId, inviter.MembershipId, ct), selected))
                return false;
        }
        foreach (var snapshot in current.AcceptedInvitations)
        {
            if (snapshot.CatalogRevision != revision
                || clock.GetUtcNow().UtcDateTime >= snapshot.ExpiresAt)
                return false;
            var invitation = await invitations.GetByIdAsync(snapshot.InvitationId, ct);
            if (invitation is null || invitation.TenantId != snapshot.TenantId
                || invitation.Version != snapshot.BeforeVersion + 1
                || invitation.Status != InvitationStatus.Accepted
                || invitation.AcceptedByUserId != snapshot.AcceptingUserId
                || !invitation.Assignments.Select(row => row.RoleId).Order().SequenceEqual(snapshot.RoleIds))
                return false;
            var inviter = await memberships.GetSnapshotByMembershipIdAsync(snapshot.Inviter.MembershipId, ct);
            if (inviter is null || !inviter.HasSameAuthorityAs(snapshot.Inviter))
                return false;
            var selected = await roles.ResolveActiveAsync(snapshot.TenantId, snapshot.RoleIds, ct);
            if (selected is null || !TenantRoleAssignmentPolicy.CanAssign(inviter,
                await roles.HasProtectedOwnerAsync(inviter.TenantId, inviter.MembershipId, ct), selected))
                return false;
        }
        return true;
    }

    private sealed class UnitOfWorkState(Guid unitOfWorkId)
    {
        public Guid UnitOfWorkId { get; } = unitOfWorkId;
        public AuthoritySnapshot? Original { get; set; }
        public long ExpectedPolicyVersion { get; set; }
        public bool? InitialIsProtectedOwner { get; set; }
        public HashSet<TenantPermission> AuthorizedPermissions { get; } = [];
        public Dictionary<Guid, MemberTransition> Members { get; } = [];
        public List<InvitationAcceptanceSnapshot> AcceptedInvitations { get; } = [];
        public List<InvitationCreationSnapshot> CreatedInvitations { get; } = [];
        public bool TenantDeleted { get; set; }
        public bool MembershipChanged { get; set; }
    }

    private sealed class MemberTransition(Guid tenantId, Guid userId, long expectedVersion)
    {
        public Guid TenantId { get; } = tenantId;
        public Guid UserId { get; } = userId;
        public long ExpectedVersion { get; set; } = expectedVersion;
        public bool Removed { get; set; }
    }

    private sealed record InvitationCreationSnapshot(
        Guid InvitationId,
        Guid TenantId,
        MembershipSnapshot Inviter,
        Guid[] RoleIds,
        DateTime ExpiresAt,
        string CatalogRevision);

    private sealed record InvitationAcceptanceSnapshot(
        Guid InvitationId,
        Guid TenantId,
        long BeforeVersion,
        Guid AcceptingUserId,
        MembershipSnapshot Inviter,
        Guid[] RoleIds,
        DateTime ExpiresAt,
        string CatalogRevision);
}
