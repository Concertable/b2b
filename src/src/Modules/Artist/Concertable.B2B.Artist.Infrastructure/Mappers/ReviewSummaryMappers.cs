using Concertable.B2B.Artist.Domain;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Infrastructure.Mappers;

internal static class ReviewSummaryMappers
{
    public static ReviewSummary ToReviewSummary(this ArtistRatingProjection? projection) =>
        projection is null
            ? new ReviewSummary(0, null)
            : new ReviewSummary(projection.ReviewCount, projection.AverageRating);
}
