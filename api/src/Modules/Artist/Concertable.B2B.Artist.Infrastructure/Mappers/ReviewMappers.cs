using Concertable.Contracts;

namespace Concertable.B2B.Artist.Infrastructure.Mappers;

internal static class ReviewMappers
{
    extension(ArtistRatingProjection? projection)
    {
        public ReviewSummary ToReviewSummary() =>
            projection is null
                ? new ReviewSummary(0, null)
                : new ReviewSummary(projection.ReviewCount, projection.AverageRating);
    }

    extension(ArtistReview review)
    {
        public ReviewDto ToReviewDto() => new()
        {
            Id = review.Id,
            Email = review.Email,
            Stars = (int)review.Stars,
            Details = review.Details
        };
    }
}
