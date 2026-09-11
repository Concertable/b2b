using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.User.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<CredentialRegisteredHandler> logger;

    public CredentialRegisteredHandler(UserDbContext context, ILogger<CredentialRegisteredHandler> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    /// <summary>Only creates the plain <see cref="UserEntity"/> projection. Admin authority is never
    /// granted here — <c>CredentialRegisteredEvent</c> fires at raw registration submission, before the
    /// account's email is verified, so granting off it would be gate-able by an unverified mailbox (see
    /// plans/launch/ADMIN_CONSOLE_PLAN.md design decision 1). The fail-closed grant instead runs from
    /// <c>AdminService.EnsureCurrentUserAdminGrantedIfEligibleAsync</c>, called from
    /// <c>UserController.Me()</c> — the first authenticated request after login, which Auth's own
    /// <c>CanAuthenticate</c> gate guarantees never happens for an unverified account.</summary>
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
        context.Users.Add(UserEntity.Create(e.UserId, e.Email));

        await context.SaveChangesAsync(ct);
        logger.WroteUserFromCredentialRegistered(e.UserId);
    }
}
