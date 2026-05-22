using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.Messaging.Domain;
using Concertable.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.User.Infrastructure.Events;

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

        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(CredentialRegisteredHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(CredentialRegisteredHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var user = UserEntity.FromRegistration(e.UserId, e.Email, role);
        context.Users.Add(user);

        if (role == Role.VenueManager)
            context.VenueManagerProfiles.Add(new VenueManagerProfileEntity(user.Id));
        else if (role == Role.ArtistManager)
            context.ArtistManagerProfiles.Add(new ArtistManagerProfileEntity(user.Id));

        await context.SaveChangesAsync(ct);
    }
}
