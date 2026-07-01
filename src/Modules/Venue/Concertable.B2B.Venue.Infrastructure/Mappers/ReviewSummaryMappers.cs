using Concertable.Contracts;
using Concertable.B2B.Venue.Domain;

namespace Concertable.B2B.Venue.Infrastructure.Mappers;

internal static class ReviewSummaryMappers
{
    public static ReviewSummary ToReviewSummary(this VenueRatingProjection? projection) =>
        projection is null
            ? new ReviewSummary(0, null)
            : new ReviewSummary(projection.ReviewCount, projection.AverageRating);
}
