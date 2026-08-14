using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Conversations.Domain.ReadModels;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Handlers;

internal sealed class ArtistParticipantProfileProjectionHandler(ConversationsDbContext context)
    : IIntegrationEventHandler<ArtistChangedEvent>
{
    public Task HandleAsync(ArtistChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default) =>
        ParticipantProfileProjection.UpsertAsync(context, e.TenantId, e.Name, e.County, e.Town, ct);
}

internal sealed class VenueParticipantProfileProjectionHandler(ConversationsDbContext context)
    : IIntegrationEventHandler<VenueChangedEvent>
{
    public Task HandleAsync(VenueChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default) =>
        e.TenantId == Guid.Empty
            ? Task.CompletedTask
            : ParticipantProfileProjection.UpsertAsync(context, e.TenantId, e.Name, e.County, e.Town, ct);
}

internal static class ParticipantProfileProjection
{
    public static async Task UpsertAsync(
        ConversationsDbContext context,
        Guid tenantId,
        string name,
        string county,
        string town,
        CancellationToken ct)
    {
        var profile = await context.ParticipantProfiles.SingleOrDefaultAsync(p => p.TenantId == tenantId, ct);

        if (profile is null)
            context.ParticipantProfiles.Add(ParticipantProfile.Create(tenantId, name, county, town));
        else
            profile.Update(name, county, town);

        await context.SaveChangesAsync(ct);
    }
}
