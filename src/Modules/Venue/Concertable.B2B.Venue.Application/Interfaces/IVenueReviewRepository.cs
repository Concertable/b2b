using Concertable.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Venue.Application.Interfaces;

internal interface IVenueReviewRepository : IReadRepository<VenueReview>
{
    Task<VenueRatingProjection?> GetRatingByVenueIdAsync(int venueId, CancellationToken ct = default);
    Task<IPagination<VenueReview>> GetPagedByVenueIdAsync(int venueId, IPageParams pageParams);
    Task<IReadOnlyList<VenueReview>> GetRecentByVenueIdAsync(
        int venueId,
        int take,
        CancellationToken ct = default);
}
