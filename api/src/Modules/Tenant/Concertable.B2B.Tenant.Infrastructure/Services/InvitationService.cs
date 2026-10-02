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
    private readonly IMembershipContext membershipContext;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly TimeProvider timeProvider;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly IOutboxUnitOfWorkBehavior unitOfWork;

    public InvitationService(
        ITenantRepository tenantRepository,
        IMembershipRepository membershipRepository,
        IInvitationRepository repository,
        ITenantContext tenantContext,
        IMembershipContext membershipContext,
        ICurrentUser currentUser,
        IUserModule userModule,
        TimeProvider timeProvider,
        IPermissionCatalog permissionCatalog,
        IOutboxUnitOfWorkBehavior unitOfWork)
    {
        this.tenantRepository = tenantRepository;
        this.membershipRepository = membershipRepository;
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.membershipContext = membershipContext;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
        this.permissionCatalog = permissionCatalog;
        this.unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<InvitationDto>> ListPendingInvitationsAsync(CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var invitations = await repository.ListPendingInvitationsByTenantAsync(tenantId, now, ct);
        return invitations
            .Select(i => new InvitationDto(i.Id, i.Email, i.Role, i.CreatedAt, i.ExpiresAt))
            .ToList();
    }

    public Task<Result<InvitationDto, InviteMemberError>> InviteAsync(
        InviteMemberRequest request,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => InviteCoreAsync(request, ct), ct);

    private async Task<Result<InvitationDto, InviteMemberError>> InviteCoreAsync(
        InviteMemberRequest request,
        CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        var tenant = await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct);
        if (tenant is null)
            return new InviteMemberError.TenantNotFound();

        var actor = await GetCurrentActorAsync(tenantId, ct);
        if (actor is not { } currentActor
            || !TenantRoleAssignmentPolicy.CanAssignRole(currentActor.Role, request.Role))
            return new InviteMemberError.NotPermitted();

        var email = request.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var members = await membershipRepository.ListMembershipsByTenantAsync(tenantId, ct);
        var memberEmails = await userModule.GetEmailsByIdsAsync(members.Select(m => m.UserId));
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
            tenantId,
            email,
            request.Role,
            currentActor.Id,
            currentActor.PermissionVersion,
            now,
            InvitationTtl);
        await repository.InsertAsync(invitation, ct);

        return new InvitationDto(invitation.Id, invitation.Email, invitation.Role, invitation.CreatedAt, invitation.ExpiresAt);
    }

    public Task<UnitResult<RevokeInvitationError>> RevokeInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => RevokeInvitationCoreAsync(invitationId, ct), ct);

    private async Task<UnitResult<RevokeInvitationError>> RevokeInvitationCoreAsync(
        Guid invitationId,
        CancellationToken ct)
    {
        var tenantId = tenantContext.GetTenantId();
        if (await tenantRepository.GetByIdForAdministrationAsync(tenantId, ct) is null)
            return new RevokeInvitationError.InvitationNotFound(invitationId);

        var invitation = await repository.GetByIdForUpdateAsync(invitationId, ct);
        if (invitation is null || invitation.TenantId != tenantId)
            return new RevokeInvitationError.InvitationNotFound(invitationId);

        var actor = await GetCurrentActorAsync(tenantId, ct);
        if (actor is null || !TenantRoleAssignmentPolicy.CanAssignRole(actor.Role, invitation.Role))
            return new RevokeInvitationError.NotPermitted();

        return invitation.Revoke().MapError(error => error.ToRevokeInvitationError());
    }

    public Task<Result<MembershipDto, AcceptInvitationError>> AcceptInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(() => AcceptInvitationCoreAsync(invitationId, ct), ct);

    private async Task<Result<MembershipDto, AcceptInvitationError>> AcceptInvitationCoreAsync(
        Guid invitationId,
        CancellationToken ct)
    {
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

        var inviter = await membershipRepository.FindMembershipByIdAsync(
            invitation.TenantId,
            invitation.InviterMembershipId,
            ct);
        if (inviter is null
            || inviter.PermissionVersion != invitation.InviterPermissionVersion
            || !TenantRoleAssignmentPolicy.CanAssignRole(inviter.Role, invitation.Role))
            return new AcceptInvitationError.InviterNotAuthorized();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var acceptance = invitation.Accept(userId, now).MapError(error => error.ToAcceptInvitationError());
        if (acceptance.TryGetError(out var error))
            return error;

        var membership = TenantMembershipEntity.Create(
            invitation.TenantId,
            userId,
            invitation.Role,
            invitation.InviterMembershipId,
            now);
        await membershipRepository.InsertAsync(membership, ct);

        return new MembershipDto(
            membership.Id,
            tenant.Id,
            tenant.LegalName,
            membership.Role,
            membership.PermissionVersion,
            [.. tenant.BusinessActivities.Where(activity => activity.IsActive).Select(activity => activity.Kind)],
            [.. permissionCatalog.For(membership.Role).Select(permission => permission.Value)]);
    }

    private async Task<TenantMembershipEntity?> GetCurrentActorAsync(Guid tenantId, CancellationToken ct)
    {
        var expected = membershipContext.Membership;
        if (expected is null || expected.TenantId != tenantId)
            return null;

        var current = await membershipRepository.FindMembershipByIdAsync(tenantId, expected.MembershipId, ct);
        return current is not null
            && current.UserId == expected.UserId
            && current.Role == expected.Role
            && current.PermissionVersion == expected.PermissionVersion
                ? current
                : null;
    }
}
