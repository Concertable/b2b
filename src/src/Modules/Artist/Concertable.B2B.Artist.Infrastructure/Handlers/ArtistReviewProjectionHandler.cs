using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Handlers;

internal sealed class ArtistReviewProjectionHandler : IIntegrationEventHandler<CustomerReviewSubmittedEvent>
{
    private readonly ArtistDbContext context;
    private readonly IBus bus;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;

    public ArtistReviewProjectionHandler(
        ArtistDbContext context,
        IBus bus,
        IOutboxUnitOfWorkBehavior outboxBehavior)
    {
        this.context = context;
        this.bus = bus;
        this.outboxBehavior = outboxBehavior;
    }

    public async Task HandleAsync(CustomerReviewSubmittedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(ArtistReviewProjectionHandler), ct))
            return;

        await outboxBehavior.ExecuteAsync(async () =>
        {
            context.AddInboxMessage(envelope, nameof(ArtistReviewProjectionHandler));

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

            context.ArtistReviews.Add(new ArtistReview
            {
                ArtistId = e.ArtistId,
                Email = e.Email,
                Stars = e.Stars,
                Details = e.Details,
                CreatedAt = envelope.OccurredAtUtc
            });

            var tenantId = await context.Artists
                .Where(a => a.Id == e.ArtistId)
                .Select(a => a.TenantId)
                .SingleOrDefaultAsync(ct);
            if (tenantId != Guid.Empty)
            {
                await bus.PublishAsync(new TenantActivityRecordedEvent(new ActivityRecord(
                    $"review:{envelope.MessageId}",
                    tenantId,
                    ActivityType.ReviewReceived,
                    envelope.OccurredAtUtc,
                    $"{e.Email} left a {e.Stars:G}-star review",
                    e.Details,
                    $"/_artist/find/artist/{e.ArtistId}")), ct);
            }

            await bus.PublishAsync(new ArtistRatingUpdatedEvent(e.ArtistId, averageRating, reviewCount), ct);
        });
    }
}
