using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.B2B.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure.Events;

internal sealed class CredentialRegisteredHandler : IIntegrationEventHandler<CredentialRegisteredEvent>
{
    private static readonly IReadOnlyDictionary<string, Role> RolesByClient = new Dictionary<string, Role>
    {
        [ClientIds.VenueWeb] = Role.VenueManager,
        [ClientIds.VenueMobile] = Role.VenueManager,
        [ClientIds.ArtistWeb] = Role.ArtistManager,
        [ClientIds.ArtistMobile] = Role.ArtistManager,
    };

    private readonly UserDbContext context;

    public CredentialRegisteredHandler(UserDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(CredentialRegisteredEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (!RolesByClient.TryGetValue(e.ClientId, out var role))
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(CredentialRegisteredHandler), ct))
            return;

        if (await context.Users.AnyAsync(u => u.Id == e.UserId, ct))
            return;

        context.AddInboxMessage(envelope, nameof(CredentialRegisteredHandler));

        var user = UserEntity.FromRegistration(e.UserId, e.Email, role);
        context.Users.Add(user);

        if (role == Role.VenueManager)
            context.VenueManagerProfiles.Add(new VenueManagerProfileEntity(user.Id));
        else if (role == Role.ArtistManager)
            context.ArtistManagerProfiles.Add(new ArtistManagerProfileEntity(user.Id));

        await context.SaveChangesAsync(ct);
    }
}
