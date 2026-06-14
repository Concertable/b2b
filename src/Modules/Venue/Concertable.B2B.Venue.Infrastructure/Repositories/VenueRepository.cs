using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenueRepository : TenantScopedRepository<VenueEntity>, IVenueRepository
{
    public VenueRepository(VenueDbContext context, ITenantContext tenant) : base(context, tenant) { }

    public async Task<VenueEntity?> GetByUserIdAsync(Guid id) =>
        await context.Venues
            .Where(v => v.UserId == id)
            .FirstOrDefaultAsync();

    public async Task<int?> GetIdByUserIdAsync(Guid userId) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.UserId == userId)
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync();

    public async Task<VenueDetails?> GetDetailsByUserIdAsync(Guid userId) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.UserId == userId)
            .ToDetails(context.VenueRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();
}
