using Concertable.B2B.Venue.Api.Responses;
using Concertable.B2B.Venue.Domain.ReadModels;

namespace Concertable.B2B.Venue.Api.Mappers;

internal static class RecentReviewMappers
{
    extension(VenueReview review)
    {
        public RecentReviewResponse ToResponse() => new(
            review.Id,
            review.Email,
            (int)review.Stars,
            review.Details,
            review.CreatedAt,
            $"/_venue/find/venue/{review.VenueId}");
    }

    extension(IEnumerable<VenueReview> reviews)
    {
        public IReadOnlyList<RecentReviewResponse> ToResponses() =>
            reviews.Select(review => review.ToResponse()).ToArray();
    }
}
