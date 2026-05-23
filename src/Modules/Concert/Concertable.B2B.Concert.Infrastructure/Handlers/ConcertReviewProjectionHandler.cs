using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Handlers;

internal class ConcertReviewProjectionHandler : IIntegrationEventHandler<CustomerReviewSubmittedEvent>
{
    private readonly ConcertDbContext context;
    private readonly IBus bus;
    private readonly IDbContextAccessor contextAccessor;

    public ConcertReviewProjectionHandler(ConcertDbContext context, IBus bus, IDbContextAccessor contextAccessor)
    {
        this.context = context;
        this.bus = bus;
        this.contextAccessor = contextAccessor;
    }

    public async Task HandleAsync(CustomerReviewSubmittedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(ConcertReviewProjectionHandler), ct))
            return;

        context.AddInboxMessage(envelope, nameof(ConcertReviewProjectionHandler));

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

        contextAccessor.Context = context;
        await bus.PublishAsync(new ConcertRatingUpdatedEvent(e.ConcertId, averageRating, reviewCount), ct);
        await context.SaveChangesAsync(ct);
    }
}
