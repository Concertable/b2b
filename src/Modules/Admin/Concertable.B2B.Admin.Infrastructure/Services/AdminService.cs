using Concertable.B2B.Admin.Application.DTOs;
using Concertable.B2B.Admin.Application.Mappers;
using Concertable.B2B.Admin.Application.Requests;
using Concertable.B2B.Admin.Infrastructure.Settings;
using Concertable.B2B.User.Contracts;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Admin.Infrastructure.Services;

internal sealed class AdminService : IAdminService
{
    private static readonly TimeSpan InvitationTtl = TimeSpan.FromDays(7);

    private readonly IAdminRepository repository;
    private readonly ICurrentUser currentUser;
    private readonly IUserModule userModule;
    private readonly TimeProvider timeProvider;
    private readonly AdminOptions adminOptions;
    private readonly ILogger<AdminService> logger;

    public AdminService(
        IAdminRepository repository,
        ICurrentUser currentUser,
        IUserModule userModule,
        TimeProvider timeProvider,
        IOptions<AdminOptions> adminOptions,
        ILogger<AdminService> logger)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.userModule = userModule;
        this.timeProvider = timeProvider;
        this.adminOptions = adminOptions.Value;
        this.logger = logger;
    }

    private async Task<IReadOnlyList<AdminDto>> ListAdminsAsync(CancellationToken ct)
    {
        var subs = await repository.ListAdminSubsAsync(ct);
        var emails = await userModule.GetEmailsByIdsAsync(subs);
        return subs
            .Select(sub => new AdminDto(sub, emails.GetValueOrDefault(sub, string.Empty)))
            .ToList();
    }

    public async Task<AdminOverview> GetOverviewAsync(CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var admins = await ListAdminsAsync(ct);

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

        var admins = await ListAdminsAsync(ct);
        if (admins.Any(a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase)))
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
            .MapError<RevokeAdminInvitationError>(error => new RevokeAdminInvitationError.RevocationFailed(error))
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

    public async Task<bool> EnsureCurrentUserAdminGrantedIfEligibleAsync(CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId || currentUser.Email is not { } email)
            return false;
        if (await repository.IsAdminAsync(userId, ct))
            return true;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var invitation = await repository.GetPendingInvitationByEmailAsync(normalizedEmail, ct);
        if (invitation is not null && invitation.IsActive(now))
        {
            invitation.Accept(userId, now);
            repository.GrantAdmin(userId);
            if (!await TrySaveGrantAsync(ct))
                return true; // a concurrent Me() call already granted the same user; not a failure

            logger.GrantedAdminProfile(userId, "invitation");
            return true;
        }

        if (string.Equals(normalizedEmail, adminOptions.BootstrapEmail, StringComparison.OrdinalIgnoreCase) &&
            await repository.CountAdminsAsync(ct) == 0)
        {
            repository.GrantAdmin(userId);
            if (!await TrySaveGrantAsync(ct))
                return true; // a concurrent Me() call already granted the same user; not a failure

            logger.GrantedAdminProfile(userId, "bootstrap");
            return true;
        }

        return false;
    }

    // EnsureCurrentUserAdminGrantedIfEligibleAsync runs on every /api/auth/me call rather than once
    // inside a single serialized registration handler, so two concurrent calls for the same
    // newly-eligible user can both pass the IsAdminAsync check above and race to grant. The loser's
    // insert hits the AdminProfiles.Sub primary key the winner just committed; treat that as the
    // natural race-loser no-op the old registration-time design got for free, not a real failure.
    private async Task<bool> TrySaveGrantAsync(CancellationToken ct)
    {
        try
        {
            await repository.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            ex.DiscardFailedChanges();
            return false;
        }
    }
}
