using Concertable.B2B.Infrastructure.Uris;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain.Errors;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class InvitationService : IInvitationService
{
    private static readonly TimeSpan InvitationTtl = TimeSpan.FromDays(7);

    private readonly ITenantRepository repository;
    private readonly ITenantContext tenantContext;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly IEmailTransport emailTransport;
    private readonly IFrontendUriGenerator uris;
    private readonly TimeProvider timeProvider;

    public InvitationService(
        ITenantRepository repository,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IUserModule userModule,
        IEmailTransport emailTransport,
        IFrontendUriGenerator uris,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.emailTransport = emailTransport;
        this.uris = uris;
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
        var tenant = await repository.GetByIdAsync(tenantId, ct);
        if (tenant is null)
            return Result.Failure<InvitationDto, InviteMemberError>(new InviteMemberError.TenantNotFound());

        var email = request.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // "Already a member" is by email; membership stores only the user id, so resolve members' emails
        // from the User projection (same batch join as the members list) and match case-insensitively.
        var members = await repository.ListMembershipsByTenantAsync(tenantId, ct);
        var memberEmails = await userModule.GetEmailsByIdsAsync(members.Select(m => m.UserId));
        if (memberEmails.Values.Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase)))
            return Result.Failure<InvitationDto, InviteMemberError>(new InviteMemberError.AlreadyMember());

        var existing = await repository.GetPendingInvitationByEmailAsync(tenantId, email, ct);
        if (existing is not null)
        {
            if (existing.IsActive(now))
                return Result.Failure<InvitationDto, InviteMemberError>(new InviteMemberError.InvitationPending());

            // A lapsed invite still holds the (TenantId, Email) filtered-unique Pending slot; retire it in its
            // own save so the new Pending row can't collide with it (the index frees only once the update lands).
            existing.Expire();
            await repository.SaveChangesAsync(ct);
        }

        var inviterId = currentUser.Id ?? throw new ForbiddenException("No authenticated user.");
        var invitation = TenantInvitationEntity.Create(tenantId, email, request.Role, inviterId, now, InvitationTtl);
        repository.AddInvitation(invitation);
        await repository.SaveChangesAsync(ct);

        await SendInvitationEmailAsync(invitation, tenant.Type);
        return Result.Success<InvitationDto, InviteMemberError>(
            new InvitationDto(invitation.Id, invitation.Email, invitation.Role, invitation.CreatedAt, invitation.ExpiresAt));
    }

    public async Task<UnitResult<RevokeInvitationError>> RevokeInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var invitation = await repository.GetInvitationByIdAsync(invitationId, ct);
        if (invitation is null || invitation.TenantId != tenantId)
            return UnitResult.Failure<RevokeInvitationError>(
                new RevokeInvitationError.InvitationNotFound(invitationId));

        return await invitation.Revoke()
            .MapError(error => error.ToRevokeInvitationError())
            .TapAsync(() => repository.SaveChangesAsync(ct));
    }

    public async Task<Result<MembershipDto, AcceptInvitationError>> AcceptInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new ForbiddenException("No authenticated user.");

        var invitation = await repository.GetInvitationByIdAsync(invitationId, ct);
        if (invitation is null)
            return Result.Failure<MembershipDto, AcceptInvitationError>(
                new AcceptInvitationError.InvitationNotFound(invitationId));

        if (string.IsNullOrWhiteSpace(currentUser.Email) ||
            !string.Equals(currentUser.Email.Trim(), invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<MembershipDto, AcceptInvitationError>(new AcceptInvitationError.EmailMismatch());
        }

        // Guard on the tenant still existing — an accept can race a tenant delete (BUG1b). Delete already
        // clears pending invitations, so this is the secondary defence against the concurrent-delete race.
        var tenant = await repository.GetByIdAsync(invitation.TenantId, ct);
        if (tenant is null)
            return Result.Failure<MembershipDto, AcceptInvitationError>(new AcceptInvitationError.TenantNotFound());

        if (await repository.IsMemberAsync(invitation.TenantId, userId, ct))
            return Result.Failure<MembershipDto, AcceptInvitationError>(new AcceptInvitationError.AlreadyMember());

        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await invitation.Accept(userId, now)
            .MapError(error => error.ToAcceptInvitationError())
            .BindAsync(async () =>
            {
                repository.AddMembership(TenantMembershipEntity.Create(
                    invitation.TenantId, userId, invitation.Role, invitedBy: invitation.CreatedByUserId, now));
                await repository.SaveChangesAsync(ct);

                return Result.Success<MembershipDto, AcceptInvitationError>(
                    new MembershipDto(tenant.Id, tenant.LegalName, tenant.Type, invitation.Role));
            });
    }

    private async Task SendInvitationEmailAsync(TenantInvitationEntity invitation, TenantType tenantType)
    {
        var acceptLink = uris.Create(tenantType, $"/settings/members/accept/{invitation.Id}");

        const string subject = "You've been invited to join an organization on Concertable";
        var body =
            $"You've been invited to join an organization on Concertable as {invitation.Role}. " +
            $"Register or sign in on the manager portal, then accept your invitation here: {acceptLink}";

        await emailTransport.SendEmailAsync(invitation.Email, subject, body);
    }
}
