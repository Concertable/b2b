using Concertable.B2B.Artist.Application.DTOs;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistReviewService
{
    Task<ReviewSummary> GetSummaryAsync(int artistId, CancellationToken ct = default);
    Task<IPagination<ReviewDto>> GetPagedAsync(int artistId, IPageParams pageParams);
    Task<IReadOnlyList<ArtistReview>> GetRecentForCurrentAsync(
        int take,
        CancellationToken ct = default);
}
