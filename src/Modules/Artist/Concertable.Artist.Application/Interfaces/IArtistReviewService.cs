using Concertable.Shared;

namespace Concertable.Artist.Application.Interfaces;

internal interface IArtistReviewService
{
    Task<ReviewSummaryDto> GetSummaryAsync(int artistId);
}
