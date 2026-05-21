using Concertable.Artist.Domain;
using Concertable.Artist.Infrastructure.Data;
using Concertable.Concert.Contracts.Events;
using Concertable.Messaging.Domain;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Artist.Infrastructure.Handlers;

internal class ArtistReviewProjectionHandler : IIntegrationEventHandler<ReviewSubmittedEvent>
{
    private readonly ArtistDbContext context;

    public ArtistReviewProjectionHandler(ArtistDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(ReviewSubmittedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(ArtistReviewProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(ArtistReviewProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var projection = await context.ArtistRatingProjections
            .FirstOrDefaultAsync(p => p.ArtistId == e.ArtistId, ct);

        if (projection is null)
        {
            context.ArtistRatingProjections.Add(new ArtistRatingProjection
            {
                ArtistId = e.ArtistId,
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
