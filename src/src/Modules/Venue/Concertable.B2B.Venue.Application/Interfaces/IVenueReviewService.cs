using Concertable.B2B.Venue.Application.DTOs;
using Concertable.Contracts;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueReviewService
{
    Task<ReviewSummary> GetSummaryAsync(int venueId);
    Task<IPagination<ReviewDto>> GetPagedAsync(int venueId, IPageParams pageParams);
    Task<IReadOnlyList<RecentReviewDto>> GetRecentForCurrentAsync(int take);
}
