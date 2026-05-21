using Concertable.Artist.Domain;
using Concertable.Shared;

namespace Concertable.Artist.Infrastructure.Mappers;

internal static class ReviewSummaryMappers
{
    public static ReviewSummaryDto ToReviewSummaryDto(this ArtistRatingProjection? projection) =>
        projection is null
            ? new ReviewSummaryDto(0, null)
            : new ReviewSummaryDto(projection.ReviewCount, projection.AverageRating);
}
