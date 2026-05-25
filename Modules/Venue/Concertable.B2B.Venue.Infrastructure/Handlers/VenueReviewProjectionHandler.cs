using Concertable.Customer.Review.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.B2B.Venue.Domain;
using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Handlers;

internal class VenueReviewProjectionHandler : IIntegrationEventHandler<CustomerReviewSubmittedEvent>
{
    private readonly VenueDbContext context;
    private readonly IBus bus;
    private readonly IDbContextAccessor contextAccessor;

    public VenueReviewProjectionHandler(VenueDbContext context, IBus bus, IDbContextAccessor contextAccessor)
    {
        this.context = context;
        this.bus = bus;
        this.contextAccessor = contextAccessor;
    }

    public async Task HandleAsync(CustomerReviewSubmittedEvent e, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VenueReviewProjectionHandler), ct))
            return;

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
            Details = e.Details
        });

        contextAccessor.Context = context;
        await bus.PublishAsync(new VenueRatingUpdatedEvent(e.VenueId, averageRating, reviewCount), ct);
        await context.SaveChangesAsync(ct);
    }
}
