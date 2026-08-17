using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistRepository : TenantScopedRepository<ArtistEntity>, IArtistRepository
{
    private readonly ArtistDbContext context;

    public ArtistRepository(ArtistDbContext context, ITenantContext tenant) : base(context, tenant)
    {
        this.context = context;
    }

    public async Task<int?> GetIdForCurrentTenantAsync() =>
        await base.CurrentTenant.AsNoTracking()
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();

    public async Task<ArtistDetails?> GetDetailsForCurrentTenantAsync() =>
        await base.CurrentTenant.AsNoTracking()
            .ToDetails(context.ArtistRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();
}
