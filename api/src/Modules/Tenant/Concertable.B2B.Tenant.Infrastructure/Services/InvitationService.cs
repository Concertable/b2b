using Concertable.B2B.Tenant.Infrastructure.Authorization;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Domain;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain.Errors;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class InvitationService : IInvitationService
{
    private static readonly TimeSpan InvitationTtl = TimeSpan.FromDays(7);

    private readonly ITenantRepository tenantRepository;
    private readonly IMembershipRepository membershipRepository;
    private readonly IInvitationRepository repository;
    private readonly ITenantContext tenantContext;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly TimeProvider timeProvider;
    private readonly IOutboxUnitOfWorkBehavior unitOfWork;
    private readonly IRoleRepository roles;
    private readonly IMembershipContext membershipContext;
    private readonly IMembershipResolver membershipResolver;
    private readonly TenantAuthorityResolver authority;
    private readonly IAuthorizationContext authorizationContext;

    public InvitationService(
        ITenantRepository tenantRepository,
        IMembershipRepository membershipRepository,
        IInvitationRepository repository,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IUserModule userModule,
        TimeProvider timeProvider,
        IOutboxUnitOfWorkBehavior unitOfWork,
        IRoleRepository roles,
        IMembershipContext membershipContext,
        IMembershipResolver membershipResolver,
        TenantAuthorityResolver authority,
        IAuthorizationContext authorizationContext)
    {
        this.tenantRepository = tenantRepository;
        this.membershipRepository = membershipRepository;
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
        this.unitOfWork = unitOfWork;
        this.roles = roles;
        this.membershipContext = membershipContext;
        this.membershipResolver = membershipResolver;
        this.authority = authority;
        this.authorizationContext = authorizationContext;
    }

    public Task<Result<IReadOnlyList<InvitationDto>, ListInvitationsError>> ListPendingInvitationsAsync(
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var tenantId = tenantContext.GetTenantId();
            if (!(await tenantRepository.GetExistingIdsForShareAsync([tenantId], ct)).Contains(tenantId)
                || await GetCurrentActorAsync(tenantId, ct) is not { } actor
                || !actor.HasPermission(TenantPermission.MembersInvite))
                return Result.Failure<IReadOnlyList<InvitationDto>, ListInvitationsError>(
                    new ListInvitationsError.NotPermitted());
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var invitations = await repository.ListPendingInvitationsByTenantAsync(tenantId, now, ct);
            var summaries = await roles.GetSummariesForInvitationsAsync(
                tenantId, invitations.Select(invitation => invitation.Id).ToArray(), ct);
            var result = new List<InvitationDto>(invitations.Count);
            foreach (var invitation in invitations)
                result.Add(new InvitationDto(invitation.Id, invitation.Email,
                    summaries.GetValueOrDefault(invitation.Id) ?? Array.Empty<RoleSummaryDto>(),
                    invitation.CreatedAt, invitation.ExpiresAt));
            return Result.Success<IReadOnlyList<InvitationDto>, ListInvitationsError>(result);
        }, ct);

    public Task<Result<InvitationDto, InviteMemberError>> InviteAsync(
        InviteMemberRequest request,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => InviteCoreAsync(request, ct), ct);

    private async Task<Result<InvitationDto, InviteMemberError>> InviteCoreAsync(
        InviteMemberRequest request,
        CancellationToken ct)
    {
        authorizationContext.RegisterFailure<Result<InvitationDto, InviteMemberError>>(
            () => Result.Failure<InvitationDto, InviteMemberError>(
                new InviteMemberError.NotPermitted()));
        var tenantId = tenantContext.GetTenantId();
        var tenant = await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct);
        if (tenant is null)
            return new InviteMemberError.TenantNotFound();

        var actor = await authority.ProveAdministrationAsync(
            tenantId, TenantPermission.MembersInvite, false, ct);
        var selectedRoles = await roles.ResolveActiveAsync(tenantId, request.RoleIds, ct);
        if (actor is null || selectedRoles is null || !await CanAssignAsync(actor, selectedRoles, ct))
            return new InviteMemberError.NotPermitted();

        var email = request.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var members = await membershipRepository.ListMembershipsByTenantAsync(tenantId, ct);
        var memberEmails = await userModule.GetEmailsByIdsAsync(members.Select(member => member.UserId));
        if (memberEmails.Values.Any(value => string.Equals(value, email, StringComparison.OrdinalIgnoreCase)))
            return new InviteMemberError.AlreadyMember();

        var existing = await repository.GetPendingInvitationByEmailAsync(tenantId, email, ct);
        if (existing is not null)
        {
            if (existing.IsActive(now))
                return new InviteMemberError.InvitationPending();
            existing.Expire();
            await repository.SaveChangesAsync(ct);
        }

        var invitation = TenantInvitationEntity.Create(
            tenantId, email, request.RoleIds, actor.MembershipId,
            actor.PermissionVersion, actor.RolePolicyVersion, now, InvitationTtl);
        if (!authority.ProveInvitationCreation(invitation, actor, selectedRoles))
            return new InviteMemberError.NotPermitted();
        await repository.InsertAsync(invitation, ct);
        return new InvitationDto(invitation.Id, invitation.Email,
            Summaries(selectedRoles), invitation.CreatedAt, invitation.ExpiresAt);
    }

    public Task<UnitResult<RevokeInvitationError>> RevokeInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => RevokeInvitationCoreAsync(invitationId, ct), ct);

    private async Task<UnitResult<RevokeInvitationError>> RevokeInvitationCoreAsync(
        Guid invitationId, CancellationToken ct)
    {
        authorizationContext.RegisterFailure<UnitResult<RevokeInvitationError>>(
            () => UnitResult.Failure<RevokeInvitationError>(
                new RevokeInvitationError.NotPermitted()));
        var tenantId = tenantContext.GetTenantId();
        if (await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct) is null)
            return new RevokeInvitationError.InvitationNotFound(invitationId);
        var invitation = await repository.GetByIdForUpdateAsync(invitationId, ct);
        if (invitation is null || invitation.TenantId != tenantId)
            return new RevokeInvitationError.InvitationNotFound(invitationId);
        if (await authority.ProveAdministrationAsync(
            tenantId, TenantPermission.MembersInvite, false, ct) is null)
            return new RevokeInvitationError.NotPermitted();
        return invitation.Revoke().MapError(error => error.ToRevokeInvitationError());
    }

    public Task<Result<MembershipDto, AcceptInvitationError>> AcceptInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => AcceptInvitationCoreAsync(invitationId, ct), ct);

    private async Task<Result<MembershipDto, AcceptInvitationError>> AcceptInvitationCoreAsync(
        Guid invitationId, CancellationToken ct)
    {
        authorizationContext.RegisterFailure<Result<MembershipDto, AcceptInvitationError>>(
            () => Result.Failure<MembershipDto, AcceptInvitationError>(
                new AcceptInvitationError.InviterNotAuthorized()));
        if (currentUser.Id is not { } userId)
            return new AcceptInvitationError.Unauthenticated();

        var invitationSnapshot = await repository.GetByIdAsync(invitationId, ct);
        if (invitationSnapshot is null)
            return new AcceptInvitationError.InvitationNotFound(invitationId);
        var tenant = await tenantRepository.GetByIdForAdministrationAsync(invitationSnapshot.TenantId, ct);
        if (tenant is null)
            return new AcceptInvitationError.TenantNotFound();
        var invitation = await repository.GetByIdForUpdateAsync(invitationId, ct);
        if (invitation is null || invitation.TenantId != tenant.Id)
            return new AcceptInvitationError.InvitationNotFound(invitationId);
        if (string.IsNullOrWhiteSpace(currentUser.Email)
            || !string.Equals(currentUser.Email.Trim(), invitation.Email, StringComparison.OrdinalIgnoreCase))
            return new AcceptInvitationError.EmailMismatch();
        if (await membershipRepository.IsMemberAsync(invitation.TenantId, userId, ct))
            return new AcceptInvitationError.AlreadyMember();
        if (invitation.Status != InvitationStatus.Pending)
            return new AcceptInvitationError.InvitationNotPending();
        if (timeProvider.GetUtcNow().UtcDateTime >= invitation.ExpiresAt)
            return new AcceptInvitationError.InvitationExpired();

        var inviter = (await membershipRepository.GetSnapshotsByIdsForShareAsync(
            [invitation.InviterMembershipId], ct)).SingleOrDefault();
        var roleIds = invitation.Assignments.Select(row => row.RoleId).ToArray();
        var selectedRoles = await roles.ResolveActiveAsync(tenant.Id, roleIds, ct);
        if (inviter is null || selectedRoles is null
            || inviter.PermissionVersion != invitation.InviterPermissionVersion
            || inviter.RolePolicyVersion != invitation.InviterRolePolicyVersion
            || !await CanAssignAsync(inviter, selectedRoles, ct)
            || !await authority.ProveInvitationAcceptanceAsync(
                invitation, inviter, selectedRoles, userId, ct))
            return new AcceptInvitationError.InviterNotAuthorized();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var acceptance = invitation.Accept(userId, now).MapError(error => error.ToAcceptInvitationError());
        if (acceptance.TryGetError(out var error))
            return error;
        var membership = TenantMembershipEntity.Create(
            tenant.Id, userId, roleIds, inviter.MembershipId, now);
        await membershipRepository.InsertAsync(membership, ct);

        return new MembershipDto(
            membership.Id, tenant.Id, tenant.LegalName,
            Summaries(selectedRoles),
            membership.PermissionVersion, tenant.RolePolicyVersion,
            [.. tenant.BusinessActivities.Where(activity => activity.IsActive).Select(activity => activity.Kind)],
            [.. selectedRoles.SelectMany(role => role.Permissions.Select(grant => grant.PermissionKey))
                .Distinct(StringComparer.Ordinal)]);
    }
    private async Task<MembershipSnapshot?> GetCurrentActorAsync(Guid tenantId, CancellationToken ct)
    {
        var expected = membershipContext.Membership;
        if (expected is null || expected.TenantId != tenantId)
            return null;
        var resolved = await membershipResolver.ResolveSnapshotAsync(expected, ct);
        return resolved.TryGetValue(out var current) ? current : null;
    }

    private async Task<bool> CanAssignAsync(
        MembershipSnapshot actor,
        IReadOnlyCollection<TenantRoleDefinition> selectedRoles,
        CancellationToken ct) =>
        TenantRoleAssignmentPolicy.CanAssign(
            actor,
            await roles.HasProtectedOwnerAsync(actor.TenantId, actor.MembershipId, ct),
            selectedRoles);

    private static IReadOnlyList<RoleSummaryDto> Summaries(
        IEnumerable<TenantRoleDefinition> selectedRoles) =>
        [.. selectedRoles.Select(role => new RoleSummaryDto(
            role.Id, role.Name, role.IsProtectedOwner))];

}
