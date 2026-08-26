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

    public InvitationService(
        ITenantRepository tenantRepository,
        IMembershipRepository membershipRepository,
        IInvitationRepository repository,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IUserModule userModule,
        TimeProvider timeProvider)
    {
        this.tenantRepository = tenantRepository;
        this.membershipRepository = membershipRepository;
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
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

    public async Task<Result<InvitationDto, InviteMemberError>> InviteAsync(
        InviteMemberRequest request,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var tenant = await tenantRepository.GetByIdAsync(tenantId, ct);
        if (tenant is null)
            return new InviteMemberError.TenantNotFound();

        var email = request.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // "Already a member" is by email; membership stores only the user id, so resolve members' emails
        // from the User projection (same batch join as the members list) and match case-insensitively.
        var members = await membershipRepository.ListMembershipsByTenantAsync(tenantId, ct);
        var memberEmails = await userModule.GetEmailsByIdsAsync(members.Select(m => m.UserId));
        if (memberEmails.Values.Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase)))
            return new InviteMemberError.AlreadyMember();

        var existing = await repository.GetPendingInvitationByEmailAsync(tenantId, email, ct);
        if (existing is not null)
        {
            if (existing.IsActive(now))
                return new InviteMemberError.InvitationPending();

            // A lapsed invite still holds the (TenantId, Email) filtered-unique Pending slot; retire it in its
            // own save so the new Pending row can't collide with it (the index frees only once the update lands).
            existing.Expire();
            await repository.SaveChangesAsync(ct);
        }

        if (currentUser.Id is not { } inviterId)
            return new InviteMemberError.Unauthenticated();

        var invitation = TenantInvitationEntity.Create(tenantId, tenant.Type, email, request.Role, inviterId, now, InvitationTtl);
        await repository.InsertAsync(invitation, ct);

        return new InvitationDto(invitation.Id, invitation.Email, invitation.Role, invitation.CreatedAt, invitation.ExpiresAt);
    }

    public async Task<UnitResult<RevokeInvitationError>> RevokeInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var invitation = await repository.GetByIdAsync(invitationId, ct);
        if (invitation is null || invitation.TenantId != tenantId)
            return new RevokeInvitationError.InvitationNotFound(invitationId);

        return await invitation.Revoke()
            .MapError(error => error.ToRevokeInvitationError())
            .TapAsync(() => repository.SaveChangesAsync(ct));
    }

    public async Task<Result<MembershipDto, AcceptInvitationError>> AcceptInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
            return new AcceptInvitationError.Unauthenticated();

        var invitation = await repository.GetByIdAsync(invitationId, ct);
        if (invitation is null)
            return new AcceptInvitationError.InvitationNotFound(invitationId);

        if (string.IsNullOrWhiteSpace(currentUser.Email) ||
            !string.Equals(currentUser.Email.Trim(), invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            return new AcceptInvitationError.EmailMismatch();
        }

        // Guard on the tenant still existing — an accept can race a tenant delete (BUG1b). Delete already
        // clears pending invitations, so this is the secondary defence against the concurrent-delete race.
        var tenant = await tenantRepository.GetByIdAsync(invitation.TenantId, ct);
        if (tenant is null)
            return new AcceptInvitationError.TenantNotFound();

        if (await membershipRepository.IsMemberAsync(invitation.TenantId, userId, ct))
            return new AcceptInvitationError.AlreadyMember();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await invitation.Accept(userId, now)
            .BindAsync(async () =>
            {
                await membershipRepository.InsertAsync(TenantMembershipEntity.Create(
                    invitation.TenantId, userId, invitation.Role, invitedBy: invitation.CreatedByUserId, now), ct);

                return Result.Success<MembershipDto, AcceptInvitationError>(
                    new MembershipDto(tenant.Id, tenant.LegalName, tenant.Type, invitation.Role));
            }, error => error.ToAcceptInvitationError());
    }
}
