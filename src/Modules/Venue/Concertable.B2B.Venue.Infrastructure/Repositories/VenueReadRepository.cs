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
        context.Venues
            .Where(venue => venue.Id == id)
            .ToProfiles()
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<VenueProfile>> GetProfilesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct = default) =>
        await context.Venues
            .Where(venue => ids.Contains(venue.Id))
            .ToProfiles()
            .ToListAsync(ct);

    public Task<VenueProfile?> GetProfileByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Venues
            .Where(venue => venue.TenantId == tenantId)
            .ToProfiles()
            .FirstOrDefaultAsync(ct);

    public Task<TenantContact?> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Venues
            .Where(venue => venue.TenantId == tenantId)
            .Select(venue => new TenantContact(venue.Name, venue.Email))
            .FirstOrDefaultAsync(ct);
}
