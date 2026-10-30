using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Infrastructure.Authorization;
using Concertable.B2B.Tenant.Domain;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

internal sealed class TenantProvisioningHandler : IIntegrationEventHandler<CredentialRegisteredEvent>
{
    private readonly TenantDbContext context;
    private readonly ITenantRepository repository;
    private readonly TimeProvider timeProvider;

    public TenantProvisioningHandler(TenantDbContext context, ITenantRepository repository, TimeProvider timeProvider)
    {
        this.context = context;
        this.repository = repository;
        this.timeProvider = timeProvider;
    }

    public async Task HandleAsync(CredentialRegisteredEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (InteractiveClientInfo.GetOrDefault(e.ClientId) is not { } client || !client.Client.ProvisionsBusinessTenant)
            return;

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(TenantProvisioningHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(TenantProvisioningHandler));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var invitedEmail = e.Email.Trim().ToLowerInvariant();
        var candidateTenantIds = await context.Invitations
            .Where(invitation =>
                invitation.Email == invitedEmail
                && invitation.Status == InvitationStatus.Pending
                && invitation.ExpiresAt > now)
            .Select(invitation => invitation.TenantId)
            .Distinct()
            .Order()
            .ToListAsync(ct);

        foreach (var tenantId in candidateTenantIds)
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 SELECT 1
                 FROM tenant."Tenants"
                 WHERE "Id" = {tenantId}
                 FOR UPDATE
                 """,
                ct);
        }

        var pendingInvitations = await context.Invitations
            .Where(invitation =>
                invitation.Email == invitedEmail
                && invitation.Status == InvitationStatus.Pending
                && invitation.ExpiresAt > now)
            .OrderBy(invitation => invitation.TenantId)
            .ToListAsync(ct);

        var acceptedInvitation = false;
        foreach (var invitation in pendingInvitations)
        {
            var inviter = await context.Memberships.SingleOrDefaultAsync(
                membership =>
                    membership.Id == invitation.InviterMembershipId
                    && membership.TenantId == invitation.TenantId,
                ct);
            var tenant = await context.Tenants.SingleAsync(row => row.Id == invitation.TenantId, ct);
            var inviterRoleIds = inviter?.Assignments.Select(row => row.RoleId).ToArray() ?? [];
            var invitedRoleIds = invitation.Assignments.Select(row => row.RoleId).ToArray();
            var allRoleIds = inviterRoleIds.Concat(invitedRoleIds).Distinct().ToArray();
            var roles = await context.RoleDefinitions.AsNoTracking()
                .Where(role => role.TenantId == invitation.TenantId && allRoleIds.Contains(role.Id)
                    && role.RetiredAt == null)
                .ToListAsync(ct);
            var inviterRoles = roles.Where(role => inviterRoleIds.Contains(role.Id)).ToArray();
            var invitedRoles = roles.Where(role => invitedRoleIds.Contains(role.Id)).ToArray();
            var permissions = inviterRoles.SelectMany(role => role.Permissions)
                .Where(row => TenantPermission.TryParse(row.PermissionKey, out _)
                    && row.Audience is ResourceAudience.AssignedResources or ResourceAudience.TenantResources)
                .GroupBy(row => row.PermissionKey, StringComparer.Ordinal)
                .ToImmutableDictionary(
                    group => TenantPermission.TryParse(group.Key, out var key) ? key : default,
                    group => (ResourceAudience)group.Max(row => (int)row.Audience));
            var snapshot = inviter is null ? null : new MembershipSnapshot(
                inviter.Id, inviter.TenantId, inviter.UserId, inviter.PermissionVersion,
                tenant.RolePolicyVersion, permissions);
            if (snapshot is null
                || snapshot.PermissionVersion != invitation.InviterPermissionVersion
                || tenant.RolePolicyVersion != invitation.InviterRolePolicyVersion
                || invitedRoles.Length != invitedRoleIds.Length
                || !TenantRoleAssignmentPolicy.CanAssign(
                    snapshot, inviterRoles.Any(role => role.IsProtectedOwner), invitedRoles))
            {
                var revocation = invitation.Revoke();
                if (revocation.TryGetError(out var revocationError))
                    throw new InvalidOperationException(revocationError.Definition.Message);
                continue;
            }

            var alreadyMember = await context.Memberships
                .AnyAsync(membership => membership.TenantId == invitation.TenantId && membership.UserId == e.UserId, ct);
            if (!alreadyMember)
            {
                context.Memberships.Add(TenantMembershipEntity.Create(
                    invitation.TenantId, e.UserId, invitedRoleIds,
                    invitation.InviterMembershipId, now));
            }

            var acceptance = invitation.Accept(e.UserId, now);
            if (acceptance.TryGetError(out var acceptanceError))
                throw new InvalidOperationException(acceptanceError.Definition.Message);
            acceptedInvitation = true;
        }

        if (!acceptedInvitation)
            await ProvisionTenantAsync(e, client.Client.InitialBusinessActivity, now, ct);

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task ProvisionTenantAsync(
        CredentialRegisteredEvent e,
        TenantBusinessActivityKind? initialActivity,
        DateTime now,
        CancellationToken ct)
    {
        var tenant = await repository.GetByCreatedByUserIdForCreationAsync(e.UserId, ct);
        if (tenant is null)
        {
            tenant = TenantEntity.Create(e.Email, e.Email, e.UserId, now);
            if (initialActivity is { } kind)
                tenant.ActivateBusinessActivity(kind, now);
            context.Tenants.Add(tenant);
        }
        else
        {
            tenant.Announce();
        }

        var hasPresets = await context.RoleDefinitions
            .AnyAsync(role => role.TenantId == tenant.Id, ct);
        if (!hasPresets)
            context.RoleDefinitions.AddRange(TenantRoleProvisioning.CreatePresets(tenant.Id));
        var hasOwnerMembership = await context.Memberships
            .AnyAsync(membership => membership.TenantId == tenant.Id && membership.UserId == e.UserId, ct);
        if (!hasOwnerMembership)
        {
            context.Memberships.Add(TenantMembershipEntity.Create(
                tenant.Id, e.UserId, [SystemPresetIds.For(tenant.Id, "Owner")], null, now));
        }
    }
}
