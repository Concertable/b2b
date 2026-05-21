using Concertable.Venue.Infrastructure.Data;
using Concertable.Venue.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Venue.Infrastructure.Repositories;

internal class VenueRepository(VenueDbContext context)
    : Repository<VenueEntity>(context), IVenueRepository
{
    public async Task<VenueEntity?> GetFullByIdAsync(int id) =>
        await context.Venues
            .Where(v => v.Id == id)
            .FirstOrDefaultAsync();

    public async Task<VenueSummaryDto?> GetSummaryAsync(int id) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.Id == id)
            .ToSummaryDto(context.VenueRatingProjections.AsNoTracking())
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

    public async Task<VenueDto?> GetDtoByIdAsync(int id) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.Id == id)
            .ToDto(context.VenueRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();

    public async Task<VenueDto?> GetDtoByUserIdAsync(Guid userId) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.UserId == userId)
            .ToDto(context.VenueRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();
}
