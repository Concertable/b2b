using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenueReadRepository : IVenueReadRepository
{
    private readonly IVenueReadDbContext context;

    public VenueReadRepository(IVenueReadDbContext context)
    {
        this.context = context;
    }

    public async Task<VenueSummary?> GetSummaryAsync(int id, CancellationToken ct = default) =>
        await context.Venues
            .Where(v => v.Id == id)
            .ToSummary(context.VenueRatingProjections)
            .FirstOrDefaultAsync(ct);

    public async Task<VenueDetails?> GetDetailsByIdAsync(int id, CancellationToken ct = default) =>
        await context.Venues
            .Where(v => v.Id == id)
            .ToDetails(context.VenueRatingProjections)
            .FirstOrDefaultAsync(ct);

    public Task<VenueProfile?> GetProfileAsync(int id, CancellationToken ct = default) =>
        Profiles().FirstOrDefaultAsync(venue => venue.Id == id, ct);

    public Task<VenueProfile?> GetProfileByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        Profiles().FirstOrDefaultAsync(venue => venue.TenantId == tenantId, ct);

    private IQueryable<VenueProfile> Profiles() =>
        context.Venues.Select(venue => new VenueProfile(
            venue.Id,
            venue.TenantId,
            venue.UserId,
            venue.Name,
            venue.About,
            venue.Email,
            venue.Address.County,
            venue.Address.Town));

}
