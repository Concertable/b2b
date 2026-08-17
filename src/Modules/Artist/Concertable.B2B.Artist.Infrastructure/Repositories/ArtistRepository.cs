using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistRepository : TenantScopedRepository<ArtistEntity>, IArtistRepository
{
    public ArtistRepository(ArtistDbContext context, ITenantContext tenant) : base(context, tenant) { }

    public async Task<ArtistEntity?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        await context.Artists.SingleOrDefaultAsync(a => a.TenantId == tenantId, ct);

    public async Task<ArtistDetails?> GetDetailsByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .ToDetails(context.ArtistRatingProjections.AsNoTracking())
            .SingleOrDefaultAsync(ct);

    public async Task<bool> ExistsByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        await context.Artists.AnyAsync(a => a.TenantId == tenantId, ct);
}
