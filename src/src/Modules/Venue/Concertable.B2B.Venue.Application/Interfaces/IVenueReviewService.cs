using Concertable.B2B.Venue.Application.DTOs;
using Concertable.Contracts;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueReviewService
{
    Task<ReviewSummary> GetSummaryAsync(int venueId, CancellationToken ct = default);
    Task<IPagination<ReviewDto>> GetPagedAsync(int venueId, IPageParams pageParams);
    Task<IReadOnlyList<VenueReview>> GetRecentForCurrentAsync(int take, CancellationToken ct = default);
}
