using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.B2B.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.User.Infrastructure.Events;

internal sealed class CredentialRegisteredHandler : IIntegrationEventHandler<CredentialRegisteredEvent>
{
    private static readonly IReadOnlyDictionary<string, Role> RolesByClient = new Dictionary<string, Role>
    {
        [ClientIds.VenueWeb] = Role.VenueManager,
        [ClientIds.VenueMobile] = Role.VenueManager,
        [ClientIds.ArtistWeb] = Role.ArtistManager,
        [ClientIds.ArtistMobile] = Role.ArtistManager,
        [ClientIds.Admin] = Role.Admin,
    };

    private readonly UserDbContext context;
    private readonly ILogger<CredentialRegisteredHandler> logger;

    public CredentialRegisteredHandler(UserDbContext context, ILogger<CredentialRegisteredHandler> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(CredentialRegisteredEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        logger.HandlingCredentialRegistered(e.UserId, e.ClientId);

        if (!RolesByClient.TryGetValue(e.ClientId, out var role))
        {
            logger.SkippedCredentialRegistered(e.UserId, $"ClientId '{e.ClientId}' is not a manager role");
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

        var user = UserEntity.FromRegistration(e.UserId, e.Email, role);
        context.Users.Add(user);

        if (role == Role.VenueManager)
            context.VenueManagerProfiles.Add(new VenueManagerProfileEntity(user.Id));
        else if (role == Role.ArtistManager)
            context.ArtistManagerProfiles.Add(new ArtistManagerProfileEntity(user.Id));
        else if (role == Role.Admin)
            context.AdminProfiles.Add(new AdminProfileEntity(user.Id));

        await context.SaveChangesAsync(ct);
        logger.WroteUserFromCredentialRegistered(e.UserId, role);
    }
}
