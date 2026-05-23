using Concertable.Contracts;
using Concertable.B2B.Venue.Domain;

namespace Concertable.B2B.Venue.Infrastructure.Mappers;

internal static class ReviewSummaryMappers
{
    public static ReviewSummaryDto ToReviewSummaryDto(this VenueRatingProjection? projection) =>
        projection is null
            ? new ReviewSummaryDto(0, null)
            : new ReviewSummaryDto(projection.ReviewCount, projection.AverageRating);
}
