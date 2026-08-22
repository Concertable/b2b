using Concertable.Customer.Review.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Handlers;

internal sealed class VenueReviewProjectionHandler : IIntegrationEventHandler<CustomerReviewSubmittedEvent>
{
    private readonly VenueDbContext context;
    private readonly IVenueRepository venueRepository;
    private readonly IBus bus;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;

    public VenueReviewProjectionHandler(
        VenueDbContext context,
        IVenueRepository venueRepository,
        IBus bus,
        IOutboxUnitOfWorkBehavior outboxBehavior)
    {
        this.context = context;
        this.venueRepository = venueRepository;
        this.bus = bus;
        this.outboxBehavior = outboxBehavior;
    }

    public async Task HandleAsync(CustomerReviewSubmittedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VenueReviewProjectionHandler), ct))
            return;

        await outboxBehavior.ExecuteAsync(async () =>
        {
            context.AddInboxMessage(envelope, nameof(VenueReviewProjectionHandler));

            var projection = await context.VenueRatingProjections
                .FirstOrDefaultAsync(p => p.VenueId == e.VenueId, ct);

            double averageRating;
            int reviewCount;

            if (projection is null)
            {
                averageRating = e.Stars;
                reviewCount = 1;
                context.VenueRatingProjections.Add(new VenueRatingProjection
                {
                    VenueId = e.VenueId,
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

            context.VenueReviews.Add(new VenueReview
            {
                VenueId = e.VenueId,
                Email = e.Email,
                Stars = e.Stars,
                Details = e.Details,
                CreatedAt = envelope.OccurredAtUtc
            });

            var tenantId = await venueRepository.GetTenantIdByIdAsync(e.VenueId, ct);
            if (tenantId is not null)
            {
                await bus.PublishAsync(new TenantActivityRecordedEvent(new ActivityRecord(
                    $"review:{envelope.MessageId}",
                    tenantId.Value,
                    ActivityType.ReviewReceived,
                    envelope.OccurredAtUtc,
                    $"{e.Email} left a {e.Stars:G}-star review",
                    e.Details,
                    $"/_venue/find/venue/{e.VenueId}")), ct);
            }

            await bus.PublishAsync(new VenueRatingUpdatedEvent(e.VenueId, averageRating, reviewCount), ct);
        });
    }
}
