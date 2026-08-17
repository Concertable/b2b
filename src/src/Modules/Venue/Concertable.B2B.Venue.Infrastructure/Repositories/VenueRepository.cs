using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenueRepository : TenantScopedRepository<VenueEntity>, IVenueRepository
{
    private readonly VenueDbContext context;

    public VenueRepository(VenueDbContext context, ITenantContext tenant) : base(context, tenant)
    {
        this.context = context;
    }

    public async Task<VenueEntity?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        await context.Venues.SingleOrDefaultAsync(v => v.TenantId == tenantId, ct);

    public async Task<VenueDetails?> GetDetailsByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        await context.Venues.AsNoTracking()
            .Where(v => v.TenantId == tenantId)
            .ToDetails(context.VenueRatingProjections.AsNoTracking())
            .SingleOrDefaultAsync(ct);

    public async Task<bool> ExistsByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        await context.Venues.AnyAsync(v => v.TenantId == tenantId, ct);
}
