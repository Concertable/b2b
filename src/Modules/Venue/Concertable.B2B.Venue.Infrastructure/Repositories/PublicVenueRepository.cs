using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class PublicVenueRepository(PublicVenueDbContext context) : IPublicVenueRepository
{
    public async Task<VenueSummary?> GetSummaryAsync(int id) =>
        await context.Venues
            .Where(v => v.Id == id)
            .ToSummary(context.VenueRatingProjections)
            .FirstOrDefaultAsync();

    public async Task<VenueDetails?> GetDetailsByIdAsync(int id) =>
        await context.Venues
            .Where(v => v.Id == id)
            .ToDetails(context.VenueRatingProjections)
            .FirstOrDefaultAsync();

    public async Task<VenueOrgIdentity?> GetOrgIdentityByTenantIdAsync(Guid tenantId) =>
        await context.Venues
            .Where(v => v.TenantId == tenantId)
            .Select(v => new VenueOrgIdentity(v.Name, v.Address.County, v.Address.Town))
            .FirstOrDefaultAsync();
}
