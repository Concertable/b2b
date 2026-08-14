using Concertable.B2B.Artist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistOrgIdentityLookup : IArtistOrgIdentityLookup
{
    private readonly ArtistDbContext context;

    public ArtistOrgIdentityLookup(ArtistDbContext context)
    {
        this.context = context;
    }

    public async Task<ArtistOrgIdentity?> GetByTenantIdAsync(Guid tenantId) =>
        await context.Artists
            .Where(a => a.TenantId == tenantId)
            .Select(a => new ArtistOrgIdentity(a.Name, a.Address.County, a.Address.Town))
            .FirstOrDefaultAsync();
}
