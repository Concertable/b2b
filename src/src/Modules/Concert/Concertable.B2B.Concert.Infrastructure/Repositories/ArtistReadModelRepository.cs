using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class ArtistReadModelRepository : IArtistReadModelRepository
{
    private readonly ConcertDbContext context;

    public ArtistReadModelRepository(ConcertDbContext context)
    {
        this.context = context;
    }

    public Task<ArtistReadModel?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        context.ArtistReadModels
            .Include(a => a.Genres)
            .SingleOrDefaultAsync(a => a.TenantId == tenantId, ct);
}
