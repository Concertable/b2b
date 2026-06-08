using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenueRepository : Repository<VenueEntity>, IVenueRepository
{
    public VenueRepository(VenueDbContext context) : base(context) { }

    public async Task<VenueSummary?> GetSummaryAsync(int id) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.Id == id)
            .ToSummary(context.VenueRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();

    public async Task<VenueEntity?> GetByUserIdAsync(Guid id) =>
        await context.Venues
            .Where(v => v.UserId == id)
            .FirstOrDefaultAsync();

    public async Task<int?> GetIdByUserIdAsync(Guid userId) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.UserId == userId)
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync();

    public async Task<VenueDetails?> GetDetailsByIdAsync(int id) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.Id == id)
            .ToDetails(context.VenueRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();

    public async Task<VenueDetails?> GetDetailsByUserIdAsync(Guid userId) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.UserId == userId)
            .ToDetails(context.VenueRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();
}
