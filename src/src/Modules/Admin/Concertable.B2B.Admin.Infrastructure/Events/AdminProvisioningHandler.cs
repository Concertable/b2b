using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Admin.Infrastructure.Data;
using Concertable.B2B.Admin.Infrastructure.Settings;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Admin.Infrastructure.Events;

/// <summary>Fail-closed admin provisioning gate (see plans/launch/ADMIN_CONSOLE_PLAN.md design decision 1):
/// grants <see cref="AdminProfileEntity"/> only for a matching pending invitation or the one-time bootstrap
/// email when no admin exists yet. Idempotent per <see cref="CredentialRegisteredEvent"/> via the inbox,
/// independent of <c>CredentialRegisteredHandler</c>'s <c>UserEntity</c> creation in the User module — the
/// FK is a plain <see cref="Guid"/>, so this needs no ordering guarantee against it.</summary>
internal sealed class AdminProvisioningHandler : IIntegrationEventHandler<CredentialRegisteredEvent>
{
    private readonly AdminDbContext context;
    private readonly TimeProvider timeProvider;
    private readonly AdminOptions adminOptions;
    private readonly ILogger<AdminProvisioningHandler> logger;

    public AdminProvisioningHandler(
        AdminDbContext context,
        TimeProvider timeProvider,
        IOptions<AdminOptions> adminOptions,
        ILogger<AdminProvisioningHandler> logger)
    {
        this.context = context;
        this.timeProvider = timeProvider;
        this.adminOptions = adminOptions.Value;
        this.logger = logger;
    }

    public async Task HandleAsync(CredentialRegisteredEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        logger.HandlingCredentialRegistered(e.UserId, e.ClientId);

        if (e.ClientId != ClientIds.Admin)
        {
            logger.SkippedCredentialRegistered(e.UserId, $"ClientId '{e.ClientId}' is not the admin client");
            return;
        }

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(AdminProvisioningHandler), ct))
        {
            logger.SkippedCredentialRegistered(e.UserId, "already in inbox");
            return;
        }

        context.AddInboxMessage(envelope, nameof(AdminProvisioningHandler));

        await GrantAdminIfEligibleAsync(e.UserId, e.Email, ct);

        await context.SaveChangesAsync(ct);
    }

    private async Task GrantAdminIfEligibleAsync(Guid userId, string email, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var invitation = await context.AdminInvitations
            .FirstOrDefaultAsync(i => i.Email == normalizedEmail && i.Status == AdminInvitationStatus.Pending, ct);
        if (invitation is not null && invitation.IsActive(now))
        {
            invitation.Accept(userId, now);
            context.AdminProfiles.Add(new AdminProfileEntity(userId));
            logger.GrantedAdminProfile(userId, "invitation");
            return;
        }

        if (string.Equals(normalizedEmail, adminOptions.BootstrapEmail, StringComparison.OrdinalIgnoreCase) &&
            !await context.AdminProfiles.AnyAsync(ct))
        {
            context.AdminProfiles.Add(new AdminProfileEntity(userId));
            logger.GrantedAdminProfile(userId, "bootstrap");
        }
    }
}
