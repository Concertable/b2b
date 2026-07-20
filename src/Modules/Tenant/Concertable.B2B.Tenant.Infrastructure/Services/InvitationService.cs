using Concertable.B2B.Tenant.Application.Requests;
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
    private readonly IEmailSender emailSender;
    private readonly IUriService uriService;
    private readonly TimeProvider timeProvider;

    public InvitationService(
        ITenantRepository repository,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IUserModule userModule,
        IEmailSender emailSender,
        IUriService uriService,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.emailSender = emailSender;
        this.uriService = uriService;
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

    public async Task<InvitationDto> InviteAsync(InviteMemberRequest request, CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var email = request.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // "Already a member" is by email; membership stores only the user id, so resolve members' emails
        // from the User projection (same batch join as the members list) and match case-insensitively.
        var members = await repository.ListMembershipsByTenantAsync(tenantId, ct);
        var memberEmails = await userModule.GetEmailsByIdsAsync(members.Select(m => m.UserId));
        if (memberEmails.Values.Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException("This person is already a member of the organization.");

        var existing = await repository.GetPendingInvitationByEmailAsync(tenantId, email, ct);
        if (existing is not null)
        {
            if (existing.IsActive(now))
                throw new ConflictException("An invitation for this email is already pending.");

            // A lapsed invite still holds the (TenantId, Email) filtered-unique Pending slot; retire it in its
            // own save so the new Pending row can't collide with it (the index frees only once the update lands).
            existing.Expire();
            await repository.SaveChangesAsync(ct);
        }

        var inviterId = currentUser.Id ?? throw new ForbiddenException("No authenticated user.");
        var invitation = TenantInvitationEntity.Create(tenantId, email, request.Role, inviterId, now, InvitationTtl);
        repository.AddInvitation(invitation);
        await repository.SaveChangesAsync(ct);

        await SendInvitationEmailAsync(invitation);
        return new InvitationDto(invitation.Id, invitation.Email, invitation.Role, invitation.CreatedAt, invitation.ExpiresAt);
    }

    public async Task RevokeInvitationAsync(Guid invitationId, CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var invitation = await repository.GetInvitationByIdAsync(invitationId, ct);
        if (invitation is null || invitation.TenantId != tenantId)
            throw new NotFoundException($"Invitation {invitationId} not found.");

        invitation.Revoke();
        await repository.SaveChangesAsync(ct);
    }

    public async Task AcceptInvitationAsync(Guid invitationId, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new ForbiddenException("No authenticated user.");

        var invitation = await repository.GetInvitationByIdAsync(invitationId, ct)
            ?? throw new NotFoundException($"Invitation {invitationId} not found.");

        if (string.IsNullOrWhiteSpace(currentUser.Email) ||
            !string.Equals(currentUser.Email.Trim(), invitation.Email, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("This invitation was issued to a different email address.");

        // Guard on the tenant still existing — an accept can race a tenant delete (BUG1b). Delete already
        // clears pending invitations, so this is the secondary defence against the concurrent-delete race.
        if (await repository.GetByIdAsync(invitation.TenantId, ct) is null)
            throw new NotFoundException("The organization for this invitation no longer exists.");

        if (await repository.IsMemberAsync(invitation.TenantId, userId, ct))
            throw new ConflictException("You are already a member of this organization.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        invitation.Accept(userId, now);
        repository.AddMembership(TenantMembershipEntity.Create(
            invitation.TenantId, userId, invitation.Role, invitedBy: invitation.CreatedByUserId, now));
        await repository.SaveChangesAsync(ct);
    }

    private async Task SendInvitationEmailAsync(TenantInvitationEntity invitation)
    {
        var acceptLink = uriService.GetUri("/settings/members/accept", new Dictionary<string, string>
        {
            ["invitationId"] = invitation.Id.ToString(),
        });

        const string subject = "You've been invited to join an organization on Concertable";
        var body =
            $"You've been invited to join an organization on Concertable as {invitation.Role}. " +
            $"Register or sign in on the manager portal, then accept your invitation here: {acceptLink}";

        await emailSender.SendEmailAsync(invitation.Email, subject, body);
    }
}
