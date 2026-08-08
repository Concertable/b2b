using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.User.Infrastructure.Events;

internal sealed class ArtistManagerSyncHandler : IIntegrationEventHandler<ArtistChangedEvent>
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private readonly UserDbContext db;

    public ArtistManagerSyncHandler(UserDbContext db)
    {
        this.db = db;
    }

    public async Task HandleAsync(ArtistChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == e.UserId, ct);
        if (user is not null)
        {
            user.SyncFromManager(
                e.Avatar,
                GeometryFactory.CreatePoint(new Coordinate(e.Longitude, e.Latitude)),
                new Address(e.County, e.Town));
        }

        await db.SaveChangesAsync(ct);
    }
}
