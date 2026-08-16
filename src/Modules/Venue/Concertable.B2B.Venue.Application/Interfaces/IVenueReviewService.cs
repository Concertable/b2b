using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Errors;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueReviewService
{
    Task<ReviewSummary> GetSummaryAsync(int venueId, CancellationToken ct = default);
    Task<IPagination<ReviewDto>> GetPagedAsync(int venueId, IPageParams pageParams);
    Task<Result<IReadOnlyList<VenueReview>, VenueError>> GetRecentForCurrentAsync(
        int take,
        CancellationToken ct = default);
}
