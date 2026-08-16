using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenueReviewRepository : ReadRepository<VenueReview>, IVenueReviewRepository
{
    private readonly VenueDbContext venueContext;

    public VenueReviewRepository(VenueDbContext context) : base(context)
    {
        venueContext = context;
    }

    public Task<VenueRatingProjection?> GetRatingByVenueIdAsync(
        int venueId,
        CancellationToken ct = default) =>
        venueContext.VenueRatingProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.VenueId == venueId, ct);

    public Task<IPagination<VenueReview>> GetPagedByVenueIdAsync(int venueId, IPageParams pageParams) =>
        venueContext.VenueReviews
            .AsNoTracking()
            .Where(r => r.VenueId == venueId)
            .OrderByDescending(r => r.Id)
            .ToPaginationAsync(pageParams);

    public async Task<IReadOnlyList<VenueReview>> GetRecentByVenueIdAsync(
        int venueId,
        int take,
        CancellationToken ct = default) =>
        await venueContext.VenueReviews
            .AsNoTracking()
            .Where(r => r.VenueId == venueId)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .ToListAsync(ct);
}
