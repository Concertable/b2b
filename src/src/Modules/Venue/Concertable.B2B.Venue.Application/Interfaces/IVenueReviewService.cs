using Concertable.Contracts;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueReviewService
{
    Task<ReviewSummary> GetSummaryAsync(int venueId);
    Task<IPagination<ReviewDto>> GetPagedAsync(int venueId, IPageParams pageParams);
}
