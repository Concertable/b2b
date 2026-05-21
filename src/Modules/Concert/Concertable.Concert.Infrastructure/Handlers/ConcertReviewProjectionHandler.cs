using Concertable.Concert.Contracts.Events;
using Concertable.Concert.Domain;
using Concertable.Concert.Infrastructure.Data;
using Concertable.Messaging.Domain;
using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Handlers;

internal class ConcertReviewProjectionHandler : IIntegrationEventHandler<ReviewSubmittedEvent>
{
    private readonly ConcertDbContext context;

    public ConcertReviewProjectionHandler(ConcertDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(ReviewSubmittedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(ConcertReviewProjectionHandler), ct))
            return;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(ConcertReviewProjectionHandler), envelope.MessageType, DateTimeOffset.UtcNow));

        var projection = await context.ConcertRatingProjections
            .FirstOrDefaultAsync(p => p.ConcertId == e.ConcertId, ct);

        if (projection is null)
        {
            context.ConcertRatingProjections.Add(new ConcertRatingProjection
            {
                ConcertId = e.ConcertId,
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
