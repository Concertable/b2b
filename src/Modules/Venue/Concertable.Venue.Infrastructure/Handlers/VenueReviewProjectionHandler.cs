using Concertable.Concert.Contracts.Events;
using Concertable.Messaging.Domain;
using Concertable.Venue.Domain;
using Concertable.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Venue.Infrastructure.Handlers;

internal class VenueReviewProjectionHandler : IIntegrationEventHandler<ReviewSubmittedEvent>
{
    private readonly VenueDbContext context;

    public VenueReviewProjectionHandler(VenueDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(ReviewSubmittedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(VenueReviewProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(VenueReviewProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var projection = await context.VenueRatingProjections
            .FirstOrDefaultAsync(p => p.VenueId == e.VenueId, ct);

        if (projection is null)
        {
            context.VenueRatingProjections.Add(new VenueRatingProjection
            {
                VenueId = e.VenueId,
                AverageRating = e.Stars,
                ReviewCount = 1
            });
        }
        else
        {
            var total = projection.AverageRating * projection.ReviewCount + e.Stars;
            projection.ReviewCount++;
            projection.AverageRating = Math.Round(total / projection.ReviewCount, 1);
        }

        await context.SaveChangesAsync(ct);
    }
}
