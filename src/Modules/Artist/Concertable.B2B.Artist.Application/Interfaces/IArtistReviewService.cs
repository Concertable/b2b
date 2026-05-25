using Concertable.Contracts;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistReviewService
{
    Task<ReviewSummaryDto> GetSummaryAsync(int artistId);
    Task<IPagination<ReviewDto>> GetPagedAsync(int artistId, IPageParams pageParams);
}
