using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IArtistReadModelRepository
{
    Task<ArtistReadModel?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default);
}
