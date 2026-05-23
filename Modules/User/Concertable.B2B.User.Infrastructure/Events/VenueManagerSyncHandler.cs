using Concertable.Messaging.Domain;
using Concertable.B2B.User.Infrastructure.Data;
using Concertable.B2B.Venue.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.User.Infrastructure.Events;

internal class VenueManagerSyncHandler : IIntegrationEventHandler<VenueChangedEvent>
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private readonly UserDbContext db;

    public VenueManagerSyncHandler(UserDbContext db)
    {
        this.db = db;
    }

    public async Task HandleAsync(VenueChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await db.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(VenueManagerSyncHandler), ct))
            return;

        db.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(VenueManagerSyncHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == e.UserId, ct);
        if (user is not null)
        {
            user.SyncFromManager(
                e.Avatar,
                GeometryFactory.CreatePoint(new Coordinate(e.Longitude, e.Latitude)),
                new Address(e.County, e.Town));
        }

        var profile = await db.VenueManagerProfiles.FirstOrDefaultAsync(p => p.Sub == e.UserId, ct);
        profile?.AssignVenue(e.VenueId);

        await db.SaveChangesAsync(ct);
    }
}
