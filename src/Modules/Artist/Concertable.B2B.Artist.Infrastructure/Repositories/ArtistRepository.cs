using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Repositories;

internal sealed class ArtistRepository : TenantScopedRepository<ArtistEntity>, IArtistRepository
{
    public ArtistRepository(ArtistDbContext context, ITenantContext tenant) : base(context, tenant) { }

    public async Task<ArtistEntity?> GetByUserIdAsync(Guid id) =>
        await context.Artists
            .Where(a => a.UserId == id)
            .FirstOrDefaultAsync();

    public async Task<int?> GetIdByUserIdAsync(Guid id) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.UserId == id)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();

    public async Task<ArtistDetails?> GetDetailsByUserIdAsync(Guid userId) =>
        await context.Artists.AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToDetails(context.ArtistRatingProjections.AsNoTracking())
            .FirstOrDefaultAsync();
}
