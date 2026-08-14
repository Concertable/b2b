using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenueOrgIdentityLookup : IVenueOrgIdentityLookup
{
    private readonly VenueDbContext context;

    public VenueOrgIdentityLookup(VenueDbContext context)
    {
        this.context = context;
    }

    public async Task<VenueOrgIdentity?> GetByTenantIdAsync(Guid tenantId) =>
        await context.Venues
            .Where(v => v.TenantId == tenantId)
            .Select(v => new VenueOrgIdentity(v.Name, v.Address.County, v.Address.Town))
            .FirstOrDefaultAsync();
}
