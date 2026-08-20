using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistRepository : ITenantScopedRepository<ArtistEntity>
{
    Task<ArtistEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<ArtistDetails?> GetDetailsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
