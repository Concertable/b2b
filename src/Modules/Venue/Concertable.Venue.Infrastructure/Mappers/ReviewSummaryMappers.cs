using Concertable.Shared;
using Concertable.Venue.Domain;

namespace Concertable.Venue.Infrastructure.Mappers;

internal static class ReviewSummaryMappers
{
    public static ReviewSummaryDto ToReviewSummaryDto(this VenueRatingProjection? projection) =>
        projection is null
            ? new ReviewSummaryDto(0, null)
            : new ReviewSummaryDto(projection.ReviewCount, projection.AverageRating);
}
