using Concertable.B2B.User.Application.DTOs;
using Concertable.B2B.User.Application.Mappers;
using Concertable.B2B.User.Application.Requests;

namespace Concertable.B2B.User.Infrastructure.Services;

internal sealed class AdminService : IAdminService
{
    private static readonly TimeSpan InvitationTtl = TimeSpan.FromDays(7);

    private readonly IAdminRepository repository;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly TimeProvider timeProvider;

    public AdminService(
        IAdminRepository repository,
        ICurrentUser currentUser,
        IUserModule userModule,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
    }

    public async Task<AdminOverview> GetOverviewAsync(CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var subs = await repository.ListAdminSubsAsync(ct);
        var emails = await userModule.GetEmailsByIdsAsync(subs);
        var admins = subs
            .Select(sub => new AdminDto(sub, emails.GetValueOrDefault(sub, string.Empty)))
            .ToList();

        var invitations = await repository.ListPendingInvitationsAsync(now, ct);
        var pending = invitations.Select(i => i.ToDto()).ToList();

        return new AdminOverview(admins, pending);
    }

    public async Task<Result<AdminInvitationDto, InviteAdminError>> InviteAsync(
        CreateAdminInvitationRequest request,
        CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var subs = await repository.ListAdminSubsAsync(ct);
        var emails = await userModule.GetEmailsByIdsAsync(subs);
        if (emails.Values.Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase)))
            return new InviteAdminError.AlreadyAdmin();

        var existing = await repository.GetPendingInvitationByEmailAsync(email, ct);
        if (existing is not null)
        {
            if (existing.IsActive(now))
                return new InviteAdminError.InvitationPending();

            // A lapsed invite still holds the Email filtered-unique Pending slot; retire it in its own save
            // so the new Pending row can't collide with it (the index frees only once the update lands).
            existing.Expire();
            await repository.SaveChangesAsync(ct);
        }

        if (currentUser.Id is not { } inviterId)
            return new InviteAdminError.Unauthenticated();

        var invitation = AdminInvitationEntity.Create(email, inviterId, now, InvitationTtl);
        await repository.InsertAsync(invitation, ct);

        return invitation.ToDto();
    }

    public async Task<UnitResult<RevokeAdminInvitationError>> RevokeInvitationAsync(
        Guid invitationId,
        CancellationToken ct = default)
    {
        var invitation = await repository.GetByIdAsync(invitationId, ct);
        if (invitation is null)
            return new RevokeAdminInvitationError.InvitationNotFound(invitationId);

        return await invitation.Revoke()
            .MapError(error => error.ToRevokeAdminInvitationError())
            .TapAsync(() => repository.SaveChangesAsync(ct));
    }

    public async Task<UnitResult<RevokeAdminError>> RevokeAdminAsync(Guid sub, CancellationToken ct = default)
    {
        if (!await repository.IsAdminAsync(sub, ct))
            return new RevokeAdminError.AdminNotFound(sub);

        // Last-admin invariant mirrors MembershipService.IsLastOwnerAsync — the platform can never lock itself out.
        if (await repository.CountAdminsAsync(ct) <= 1)
            return new RevokeAdminError.LastAdmin();

        repository.RemoveAdmin(sub);
        await repository.SaveChangesAsync(ct);
        return new Success();
    }

    public Task<bool> IsCurrentUserAdminAsync(CancellationToken ct = default) =>
        currentUser.Id is { } id ? repository.IsAdminAsync(id, ct) : Task.FromResult(false);
}
