using Concertable.B2B.Artist.Application.DTOs;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistReviewService
{
    Task<ReviewSummary> GetSummaryAsync(int artistId);
    Task<IPagination<ReviewDto>> GetPagedAsync(int artistId, IPageParams pageParams);
    Task<IReadOnlyList<RecentReviewDto>> GetRecentForCurrentAsync(int take);
}
