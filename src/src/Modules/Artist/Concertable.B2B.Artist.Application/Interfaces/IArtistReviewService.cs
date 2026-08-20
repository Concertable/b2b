using Concertable.B2B.Artist.Application.DTOs;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistReviewService
{
    Task<ReviewSummary> GetSummaryAsync(int artistId, CancellationToken ct = default);
    Task<IPagination<ReviewDto>> GetPagedAsync(int artistId, IPageParams pageParams);
    Task<Option<IReadOnlyList<ArtistReview>>> GetRecentForCurrentAsync(
        int take,
        CancellationToken ct = default);
}
