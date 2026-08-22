using Concertable.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistReviewRepository : IReadRepository<ArtistReview>
{
    Task<ArtistRatingProjection?> GetRatingByArtistIdAsync(int artistId, CancellationToken ct = default);
    Task<IPagination<ArtistReview>> GetPagedByArtistIdAsync(int artistId, IPageParams pageParams);
    Task<IReadOnlyList<ArtistReview>> GetRecentByArtistIdAsync(int artistId, int take, CancellationToken ct = default);
}
