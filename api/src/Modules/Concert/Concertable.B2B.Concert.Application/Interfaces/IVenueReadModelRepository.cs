using Concertable.B2B.Concert.Domain.ReadModels;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IVenueReadModelRepository
{
    Task<VenueReadModel?> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken ct = default);
}
