using Concertable.B2B.Concert.Domain;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Concertable.B2B.Venue.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Concert.Infrastructure.Handlers;

internal class VenueReadModelProjectionHandler : IIntegrationEventHandler<VenueChangedEvent>
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private readonly ConcertDbContext context;

    public VenueReadModelProjectionHandler(ConcertDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(VenueChangedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VenueReadModelProjectionHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(VenueReadModelProjectionHandler));

        var venue = await context.VenueReadModels.FirstOrDefaultAsync(v => v.Id == e.VenueId, ct);
        var location = GeometryFactory.CreatePoint(new Coordinate(e.Longitude, e.Latitude));

        if (venue is null)
        {
            context.VenueReadModels.Add(new VenueReadModel
            {
                Id = e.VenueId,
                UserId = e.UserId,
                Name = e.Name,
                About = e.About,
                County = e.County,
                Town = e.Town,
                Location = location
            });
        }
        else
        {
            venue.UserId = e.UserId;
            venue.Name = e.Name;
            venue.About = e.About;
            venue.County = e.County;
            venue.Town = e.Town;
            venue.Location = location;
        }

        await context.SaveChangesAsync(ct);
    }
}
