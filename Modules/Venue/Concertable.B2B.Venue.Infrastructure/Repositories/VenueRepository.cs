using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenueRepository : TenantScopedRepository<VenueEntity>, IVenueRepository
{
    public VenueRepository(VenueDbContext context, ITenantContext tenant) : base(context, tenant) { }

    public async Task<int?> GetIdForCurrentTenantAsync() =>
        await CurrentTenant.AsNoTracking()
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync();

    public async Task<VenueDetails?> GetDetailsForCurrentTenantAsync() =>
        await CurrentTenant.AsNoTracking()
            .ToDetails(context.VenueRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();
}
