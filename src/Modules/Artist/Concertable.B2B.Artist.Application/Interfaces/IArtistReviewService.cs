using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Errors;
using Concertable.Contracts;
using Reunion;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistReviewService
{
    Task<ReviewSummary> GetSummaryAsync(int artistId, CancellationToken ct = default);
    Task<IPagination<ReviewDto>> GetPagedAsync(int artistId, IPageParams pageParams);
    Task<Result<IReadOnlyList<ArtistReview>, ArtistError>> GetRecentForCurrentAsync(
        int take,
        CancellationToken ct = default);
}
