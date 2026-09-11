using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class VenueReadModelRepository : IVenueReadModelRepository
{
    private readonly ConcertDbContext context;

    public VenueReadModelRepository(ConcertDbContext context)
    {
        this.context = context;
    }

    public Task<VenueReadModel?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        context.VenueReadModels.SingleOrDefaultAsync(v => v.TenantId == tenantId, ct);
}
