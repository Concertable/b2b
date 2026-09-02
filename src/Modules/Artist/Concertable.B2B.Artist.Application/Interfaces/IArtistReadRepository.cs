using Concertable.B2B.Artist.Application.DTOs;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Application.Interfaces;

internal interface IArtistReadRepository
{
    Task<ArtistSummary?> GetSummaryAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ArtistSummary>> GetSummariesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct = default);
    Task<ArtistDetails?> GetDetailsByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlySet<Genre>> GetGenresAsync(int id, CancellationToken ct = default);
    Task<ArtistDto?> GetProfileAsync(int id, CancellationToken ct = default);
    Task<ArtistDto?> GetProfileByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantContact?> GetContactByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
