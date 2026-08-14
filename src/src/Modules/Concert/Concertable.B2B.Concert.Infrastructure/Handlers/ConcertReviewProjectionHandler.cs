using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Handlers;

internal sealed class ConcertReviewProjectionHandler : IIntegrationEventHandler<CustomerReviewSubmittedEvent>
{
    private readonly ConcertTenantDbContext context;
    private readonly IBus bus;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;

    public ConcertReviewProjectionHandler(ConcertTenantDbContext context, IBus bus, IOutboxUnitOfWorkBehavior outboxBehavior)
    {
        this.context = context;
        this.bus = bus;
        this.outboxBehavior = outboxBehavior;
    }

    public async Task HandleAsync(CustomerReviewSubmittedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(ConcertReviewProjectionHandler), ct))
            return;

        await outboxBehavior.ExecuteAsync(async () =>
        {
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

            await bus.PublishAsync(new ConcertRatingUpdatedEvent(e.ConcertId, averageRating, reviewCount), ct);
        });
    }
}
