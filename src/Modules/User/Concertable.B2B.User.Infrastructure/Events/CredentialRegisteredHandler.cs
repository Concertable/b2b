using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.User.Infrastructure.Data;
using Concertable.B2B.User.Infrastructure.Settings;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.User.Infrastructure.Events;

internal sealed class CredentialRegisteredHandler : IIntegrationEventHandler<CredentialRegisteredEvent>
{
    private static readonly IReadOnlySet<string> ManagerClientIds = new HashSet<string>
    {
        ClientIds.VenueWeb,
        ClientIds.VenueMobile,
        ClientIds.ArtistWeb,
        ClientIds.ArtistMobile,
        ClientIds.Admin,
    };

    private readonly UserDbContext context;
    private readonly TimeProvider timeProvider;
    private readonly AdminOptions adminOptions;
    private readonly ILogger<CredentialRegisteredHandler> logger;

    public CredentialRegisteredHandler(
        UserDbContext context,
        TimeProvider timeProvider,
        IOptions<AdminOptions> adminOptions,
        ILogger<CredentialRegisteredHandler> logger)
    {
        this.context = context;
        this.timeProvider = timeProvider;
        this.adminOptions = adminOptions.Value;
        this.logger = logger;
    }

    public async Task HandleAsync(CredentialRegisteredEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        logger.HandlingCredentialRegistered(e.UserId, e.ClientId);

        if (!ManagerClientIds.Contains(e.ClientId))
        {
            logger.SkippedCredentialRegistered(e.UserId, $"ClientId '{e.ClientId}' is not a manager client");
            return;
        }

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(CredentialRegisteredHandler), ct))
        {
            logger.SkippedCredentialRegistered(e.UserId, "already in inbox");
            return;
        }

        if (await context.Users.AnyAsync(u => u.Id == e.UserId, ct))
        {
            logger.SkippedCredentialRegistered(e.UserId, "user already exists");
            return;
        }

        context.AddInboxMessage(envelope, nameof(CredentialRegisteredHandler));

        var user = UserEntity.FromRegistration(e.UserId, e.Email);
        context.Users.Add(user);

        if (e.ClientId == ClientIds.Admin)
            await GrantAdminIfEligibleAsync(user, e.Email, ct);

        await context.SaveChangesAsync(ct);
        logger.WroteUserFromCredentialRegistered(e.UserId);
    }

    /// <summary>Fail-closed admin provisioning gate (see plans/launch/ADMIN_CONSOLE_PLAN.md design decision 1):
    /// grants <see cref="AdminProfileEntity"/> only for a matching pending invitation or the one-time bootstrap
    /// email when no admin exists yet. Every other registration through the admin client still creates a plain
    /// <see cref="UserEntity"/> above, but is provably inert for authority.</summary>
    private async Task GrantAdminIfEligibleAsync(UserEntity user, string email, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var invitation = await context.AdminInvitations
            .FirstOrDefaultAsync(i => i.Email == normalizedEmail && i.Status == AdminInvitationStatus.Pending, ct);
        if (invitation is not null && invitation.IsActive(now))
        {
            invitation.Accept(user.Id, now);
            context.AdminProfiles.Add(new AdminProfileEntity(user.Id));
            logger.GrantedAdminProfile(user.Id, "invitation");
            return;
        }

        if (string.Equals(normalizedEmail, adminOptions.BootstrapEmail, StringComparison.OrdinalIgnoreCase) &&
            !await context.AdminProfiles.AnyAsync(ct))
        {
            context.AdminProfiles.Add(new AdminProfileEntity(user.Id));
            logger.GrantedAdminProfile(user.Id, "bootstrap");
        }
    }
}
