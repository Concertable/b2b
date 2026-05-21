using Concertable.Concert.Contracts.Events;
using Concertable.Concert.Domain;
using Concertable.Concert.Infrastructure.Data;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Handlers;

internal class ConcertReviewProjectionHandler : IIntegrationEventHandler<ReviewSubmittedEvent>
{
    private readonly ConcertDbContext context;
    private readonly IBus bus;

    public ConcertReviewProjectionHandler(ConcertDbContext context, IBus bus)
    {
        this.context = context;
        this.bus = bus;
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

        double averageRating;
        int reviewCount;

        if (projection is null)
        {
            averageRating = e.Stars;
            reviewCount = 1;
            context.ConcertRatingProjections.Add(new ConcertRatingProjection
            {
                ConcertId = e.ConcertId,
                AverageRating = averageRating,
                ReviewCount = reviewCount
            });
        }
        else
        {
            var total = projection.AverageRating * projection.ReviewCount + e.Stars;
            reviewCount = projection.ReviewCount + 1;
            averageRating = Math.Round(total / reviewCount, 1);
            projection.ReviewCount = reviewCount;
            projection.AverageRating = averageRating;
        }

        await bus.PublishAsync(new ConcertRatingUpdatedEvent(e.ConcertId, averageRating, reviewCount), ct);
        await context.SaveChangesAsync(ct);
    }
}
