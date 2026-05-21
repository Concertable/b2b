using Concertable.Artist.Contracts.Events;
using Concertable.Artist.Domain;
using Concertable.Artist.Infrastructure.Data;
using Concertable.Concert.Contracts.Events;
using Concertable.Messaging.Domain;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Artist.Infrastructure.Handlers;

internal class ArtistReviewProjectionHandler : IIntegrationEventHandler<ReviewSubmittedEvent>
{
    private readonly ArtistDbContext context;
    private readonly IBus bus;
    private readonly IDbContextAccessor contextAccessor;

    public ArtistReviewProjectionHandler(ArtistDbContext context, IBus bus, IDbContextAccessor contextAccessor)
    {
        this.context = context;
        this.bus = bus;
        this.contextAccessor = contextAccessor;
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

        double averageRating;
        int reviewCount;

        if (projection is null)
        {
            averageRating = e.Stars;
            reviewCount = 1;
            context.ArtistRatingProjections.Add(new ArtistRatingProjection
            {
                ArtistId = e.ArtistId,
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

        contextAccessor.Context = context;
        await bus.PublishAsync(new ArtistRatingUpdatedEvent(e.ArtistId, averageRating, reviewCount), ct);
        await context.SaveChangesAsync(ct);
    }
}
