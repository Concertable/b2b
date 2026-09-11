using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.Artist.Domain.ReadModels;

namespace Concertable.B2B.Artist.Api.Mappers;

internal static class RecentReviewMappers
{
    extension(ArtistReview review)
    {
        public RecentReviewResponse ToResponse() => new(
            review.Id,
            review.Email,
            (int)review.Stars,
            review.Details,
            review.CreatedAt,
            $"/_artist/find/artist/{review.ArtistId}");
    }

    extension(IEnumerable<ArtistReview> reviews)
    {
        public IReadOnlyList<RecentReviewResponse> ToResponses() =>
            reviews.Select(review => review.ToResponse()).ToArray();
    }
}
