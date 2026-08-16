using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistRepository : TenantScopedRepository<ArtistEntity>, IArtistRepository
{
    public ArtistRepository(ArtistDbContext context, ITenantContext tenant) : base(context, tenant) { }

    public async Task<int?> GetIdForCurrentTenantAsync() =>
        await base.CurrentTenant.AsNoTracking()
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();

    public async Task<ArtistDetails?> GetDetailsForCurrentTenantAsync() =>
        await base.CurrentTenant.AsNoTracking()
            .ToDetails(context.ArtistRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();
}
